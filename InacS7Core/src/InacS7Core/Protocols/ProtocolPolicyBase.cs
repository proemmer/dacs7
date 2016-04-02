using InacS7Core.Arch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace InacS7Core.Helper
{
    public abstract class ProtocolPolicyBase : IProtocolPolicy
    {
        #region HelperClass
        private class FoundMarker
        {
            internal FoundMarker(byte[] marker, PatternMatch<byte>.Result result, Marker metaData)
            {
                Marker = marker; Result = result;
                MetaData = metaData;
            }
            public byte[] Marker { get; private set; }
            public PatternMatch<byte>.Result Result { get; private set; }
            public Marker MetaData { get; private set; }
        }


        private class ProtocolBinding
        {
            public ProtocolPolicyBase ProtocolPolicy { get; set; }

        }
        #endregion

        #region Fields
        private static readonly ReaderWriterLockSlim CacheLock = new ReaderWriterLockSlim();
        private static readonly Dictionary<Type, ProtocolBinding> Bindings = new Dictionary<Type, ProtocolBinding>();
        private enum EProcessingState { SyncingOffset, SyncingLength, Collecting };
        private readonly List<Marker> _markers = new List<Marker>();
        private readonly List<Tuple<byte[], Marker>> _beginMarkerSequences = new List<Tuple<byte[], Marker>>();
        private readonly List<Tuple<byte[], Marker>> _endMarkerSequences = new List<Tuple<byte[], Marker>>();
        private static List<ProtocolPolicyBase> _orderedBindingsCache = null;
        //private ILogger logger = NullLogger.Instance;
        #endregion

        #region Policy Factory 
        public static IProtocolPolicy FindPolicyByPayload(IEnumerable<byte> data)
        {
            CacheLock.EnterUpgradeableReadLock();
            try
            {
                if (_orderedBindingsCache == null)
                {
                    CacheLock.EnterWriteLock();
                    try
                    {
                        _orderedBindingsCache = Bindings.Values
                            .OrderByDescending(x => x.ProtocolPolicy.BeginMarkerEnd)
                            .Select(binding => binding.ProtocolPolicy).ToList();
                    }
                    finally
                    {
                        CacheLock.ExitWriteLock();
                    }
                }
                return _orderedBindingsCache.FirstOrDefault(x => x.Test(data));
            }
            finally
            {
                CacheLock.ExitUpgradeableReadLock();
            }

        }

        #endregion

        protected ProtocolPolicyBase()
        {
            if (!Bindings.ContainsKey(GetType()))
            {
                Bindings.Add(GetType(), new ProtocolBinding()
                {
                    ProtocolPolicy = this
                });

                if (_orderedBindingsCache == null)
                    return;
                CacheLock.EnterWriteLock();
                try
                {
                    _orderedBindingsCache = null;
                }
                finally
                {
                    CacheLock.ExitWriteLock();
                }
            }
        }

        #region Public Methods (for setup of the class)

        protected void AddMarker(IEnumerable<byte> aByteSequence, int aOffsetInStream, bool aEndMarker, bool aExclusiveMarker = false)
        {
            AddMarker(new Marker(aByteSequence, aOffsetInStream, aEndMarker, aExclusiveMarker));
        }

        protected void AddMarker(Marker aMarker)
        {
            _markers.Add(aMarker);
            if (aMarker.IsEndMarker)
            {
                if (_endMarkerSequences.Count == 1)
                    throw new Exception("Only one End marker allowed!");
                _endMarkerSequences.Add(new Tuple<byte[], Marker>(aMarker.ByteSequence.ToArray(), aMarker));
            }
            else
                _beginMarkerSequences.Add(new Tuple<byte[], Marker>(aMarker.ByteSequence.ToArray(), aMarker));

        }

        public int NumberOfBeginMarkers
        {
            get
            {
                return _markers.Count(x => !x.IsEndMarker);
            }
        }

        public int BeginMarkerLength
        {
            get
            {
                return _markers.Where(x => !x.IsEndMarker).Sum(x => x.SequenceLength);
            }
        }

        public int BeginMarkerEnd
        {
            get
            {
                return _markers.Where(x => !x.IsEndMarker).Max(x => x.OffsetInStream + x.SequenceLength);
            }
        }

        public int EndMarkerLength
        {
            get
            {
                return _markers.Where(x => x.IsEndMarker).Sum(x => x.SequenceLength);
            }
        }
        #endregion

        #region Abstract Methods
        public abstract int GetMinimumCountDataBytes();                  // e.g. HeaderLength / Fix dataLength / Beginmarker Length + EndMarker Length /
        public abstract int GetDatagramLength(IEnumerable<byte> data);   // Header + Payload
        public abstract void SetupMessageAttributes(IMessage message);
        public abstract IEnumerable<byte> CreateRawMessage(IMessage message);
        public abstract IEnumerable<byte> CreateReply(IMessage message, object error = null);
        #endregion

        #region Interface Implementation
        public ExtractionResult ExtractRawMessages(IEnumerable<byte> data)
        {
            var buffer = new List<byte>(data);   //create a Copy
            var dataLength = buffer.Count();
            var rawMessageList = InspectAndExtract(buffer);
            var bytesLeft = buffer.Count();
            return new ExtractionResult(dataLength - bytesLeft, bytesLeft != 0 ? GetMinimumCountDataBytes() : 0, rawMessageList);
        }

        public IEnumerable<IMessage> Normalize(string origin, IEnumerable<IEnumerable<byte>> rawMessages)
        {
            foreach (var message in rawMessages.Select(rawMessage => Message.CreateFromRawMessage(origin, this, rawMessage)))
            {
                SetupMessageAttributes(message);
                yield return message;
            }
        }


        public IEnumerable<IEnumerable<byte>> TranslateToRawMessage(IMessage message, bool withoutCheck = false)
        {
            var raw = message.GetRawMessage();
            if (raw != null)
                return new List<IEnumerable<byte>> { raw };
            //create RawMessage from Attributes
            raw = CreateRawMessage(message);

            //recheck creation
            if (withoutCheck)
                return new List<IEnumerable<byte>> { raw };

            var rawMessageList = InspectAndExtract(raw);
            if (rawMessageList.Any())
                return rawMessageList;
            throw new Exception("RawMessage could be Translated from Attributes.");
        }

        public IMessage CreateReplyMessage(IMessage message)
        {
            var reply = message.GetReplyMessage();
            if (reply != null)
            {
                var rawMsg = reply.GetRawMessage();
                if (rawMsg == null)
                {
                    reply = Message.CreateFromRawMessage(message.GetOrigin(), this, TranslateToRawMessage(reply).FirstOrDefault());
                }
                return ReplyMessage.Create(reply);
            }

            return ReplyMessage.Create(
                Message.CreateFromRawMessage(message.GetOrigin(),
                                                    this,
                                                    CreateReply(message, message.GetAttribute<object>("$Error", null))));

        }


        public IEnumerable<KeyValuePair<IMessage, IMessage>> MatchCorrelatedMessages(IEnumerable<IMessage> requestMessages, IEnumerable<IMessage> replyMessages)
        {
            throw new NotImplementedException();
            //IEnumerable<KeyValuePair<IMessage, IMessage>> result = new List<KeyValuePair<IMessage, IMessage>>();
            //return result;
        }

        #endregion

        #region Private Methods

        private static FoundMarker FindDatagramMarker(IEnumerable<byte> data, IEnumerable<Tuple<byte[], Marker>> markers)
        {
            var patternMatchResults = new List<FoundMarker>();
            var aCollection = data as byte[] ?? data.ToArray();
            var firstOffset = 0;
            foreach (var marker in markers)
            {
                var offset = marker.Item2.OffsetInStream;
                var result = PatternMatch<byte>.MatchOrMatchPartiallyAtEnd(aCollection, marker.Item1, firstOffset + offset);
                if (!result.NoMatch)
                {
                    if (!patternMatchResults.Any())
                        firstOffset = result.MatchPos - offset;
                    patternMatchResults.Add(new FoundMarker(marker.Item1, result, marker.Item2));
                }
                else
                    break; //if we have no marker, then 
            }

            if (patternMatchResults.Count == 0)
                return null;

            if (patternMatchResults.Count > 1)
            {
                // sort results: first the full matches then the partial matches, then by position
                patternMatchResults.Sort((m1, m2) =>
                {
                    if (m1.Result.FullMatch && !m2.Result.FullMatch)
                        return -1;
                    if (m2.Result.FullMatch && !m1.Result.FullMatch)
                        return 1;
                    return m1.Result.MatchPos - m2.Result.MatchPos;
                });
            }

            return patternMatchResults.First();
        }

        private static int Eat(List<byte> data, int offsetNextValid)
        {
            var length = data.Count;
            if (offsetNextValid < 0)
            {
                data.Clear();
            }
            else
            {
                //LogEaten(data, offsetNextValid);
                if (length <= offsetNextValid)
                    data.Clear();
                else
                    data.RemoveRange(0, offsetNextValid);
            }
            return length - data.Count;
        }

        private void LogEaten(List<byte> data, int count)
        {
            if (count == 0)
                return;

            var minCount = Math.Min(data.Count(), count);
            var sb = new StringBuilder();
            foreach (var item in data.Take(minCount))
                sb.AppendFormat("{0:X2} ", item);

            if (minCount > 0)
                sb.Length = sb.Length - 1;

            var asString = Encoding.ASCII.GetString(data.ToArray(), 0, minCount);
        }

        private IEnumerable<IEnumerable<byte>> InspectAndExtract(IEnumerable<byte> data)
        {
            var processingState = EProcessingState.SyncingOffset;
            var originData = data as List<byte>;
            var messages = new List<List<byte>>();
            Marker offsetMarker = null;
            Marker lengthMarker = null;

            var ready = false;
            while (!ready)
            {
                switch (processingState)
                {
                    case EProcessingState.SyncingOffset:
                        {
                            if (_beginMarkerSequences.Count > 0)
                            {
                                var foundMarker = FindDatagramMarker(originData, _beginMarkerSequences);

                                if (foundMarker != null && foundMarker.Result != null)
                                {
                                    // Find the marker in the marker list by it's byte sequence.
                                    //offsetMarker = _markers.Find(m => !m.ByteSequence.SequenceEqual(foundMarker.Marker));
                                    offsetMarker = foundMarker.MetaData;
                                    var offsetInStream = offsetMarker.OffsetInStream;

                                    if (foundMarker.Result.FullMatch)
                                    {
                                        if (foundMarker.Result.MatchPos > offsetInStream)
                                        {
                                            // read away up to sync
                                            Eat(originData, foundMarker.Result.MatchPos - offsetInStream);
                                        }
                                        processingState = EProcessingState.SyncingLength;
                                    }
                                    else if (foundMarker.Result.PartialMatch)
                                    {
                                        if (foundMarker.Result.MatchPos > offsetInStream)
                                        {
                                            // read away up to partial (potential) match position
                                            Eat(originData, foundMarker.Result.MatchPos - offsetInStream);
                                        }
                                        ready = true;
                                    }
                                    else
                                    {
                                        Debug.Assert(false);
                                        ready = true;
                                    }
                                }
                                else
                                {
                                    Eat(originData, -1);
                                    ready = true;
                                }
                            }
                            else
                            {
                                //No Marker
                                processingState = EProcessingState.SyncingLength;
                            }
                        }
                        break;

                    case EProcessingState.SyncingLength:
                        {
                            if (originData != null && originData.Count >= GetMinimumCountDataBytes())
                            {
                                processingState = EProcessingState.Collecting;
                            }
                            else
                                ready = true;
                        }
                        break;

                    case EProcessingState.Collecting:
                        {
                            var length = originData != null ? originData.Count : -1;
                            var datagramLength = int.MaxValue;

                            if (_endMarkerSequences.Count > 0)
                            {
                                //Logger.DebugFormat("InspectAndExtract '{0}': Collecting data with the usage of EndMarker", protocolName);
                                var foundMarker = FindDatagramMarker(originData, _endMarkerSequences);
                                if (foundMarker != null && foundMarker.Result != null)
                                {
                                    lengthMarker = foundMarker.MetaData;
                                    if (foundMarker.Result.FullMatch)
                                        datagramLength = foundMarker.Result.MatchPos + lengthMarker.SequenceLength;
                                }
                            }
                            else
                            {
                                datagramLength = GetDatagramLength(originData);
                            }


                            //Logger.DebugFormat("InspectAndExtract '{0}': Collecting data for DatagramLength <{1}>", protocolName, datagramLength);
                            if (originData != null && datagramLength > 0 && datagramLength <= length)
                            {
                                try
                                {
                                    var buffer = new List<byte>(originData.Take(datagramLength));
                                    //Remove the First and The last Marker if they are Exclusive Markers
                                    if (lengthMarker != null && lengthMarker.IsExclusiveMarker)
                                        buffer.RemoveRange(lengthMarker.OffsetInStream, lengthMarker.SequenceLength);

                                    if (offsetMarker != null && offsetMarker.IsExclusiveMarker)
                                        buffer.RemoveRange(offsetMarker.OffsetInStream, offsetMarker.SequenceLength);

                                    processingState = EProcessingState.SyncingOffset;
                                    originData.RemoveRange(0, datagramLength);

                                    messages.Add(buffer);
                                }
                                catch (Exception)
                                {
                                    Eat(originData, datagramLength);
                                    processingState = EProcessingState.SyncingOffset;
                                    ready = true;
                                }
                            }
                            else
                            {
                                if (datagramLength <= 0) // Invalid headers, because header and data length are not ascertainable.
                                {
                                    processingState = EProcessingState.SyncingOffset;
                                    originData.RemoveRange(0, GetMinimumCountDataBytes());
                                }
                                ready = true;
                            }
                        }
                        break;
                }
            }

            //Logger.DebugFormat("InspectAndExtract '{0}': finished with {1} extracted messages", protocolName, messages.Count);
            return messages;
        }

        private bool Test(IEnumerable<byte> data)
        {
            var payload = data.ToArray();
            if (payload.Length < GetMinimumCountDataBytes())
                return false;

            foreach (var marker in _markers.Where(x => !x.IsEndMarker))
            {
                if (payload.Length < (marker.OffsetInStream + marker.SequenceLength))
                    return false;

                if (!payload.Skip(marker.OffsetInStream).Take(marker.SequenceLength).SequenceEqual(marker.ByteSequence))
                    return false;
            }

            return true;
        }

        private static IEnumerable<Type> FindDerivedTypesFromAssembly(Assembly assembly, Type baseType, bool classOnly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly", "Assembly must be defined");

            if (baseType == null)
                throw new ArgumentNullException("baseType", "Parent Type must be defined");


            // get all the types
            var types = assembly.GetTypes();
            var bti = baseType.GetTypeInfo();

            // works out the derived types
            foreach (var type in types)
            {
                var ti = type.GetTypeInfo();
                // if classOnly, it must be a class
                // useful when you want to create instance
                if (classOnly && !ti.IsClass)
                    continue;

                if (bti.IsInterface)
                {
                    var it = type.GetInterfaces().FirstOrDefault(x => x.Name == baseType.FullName);

                    if (it != null)
                        // add it to result list
                        yield return type;
                }
                else if (type.GetTypeInfo().IsSubclassOf(baseType))
                {
                    // add it to result list
                    yield return type;
                }
            }
        }


        #endregion
    }

}
