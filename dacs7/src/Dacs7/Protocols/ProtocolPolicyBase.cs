using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Dacs7.Helper
{
    public abstract class ProtocolPolicyBase : IProtocolPolicy
    {
        #region HelperClass

        /// <summary>
        /// This class is used to store temporary matching results to use it later.
        /// </summary>
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

        /// <summary>
        /// This is a binding helper class, if necessary we can store more data for the binding
        /// </summary>
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

        /// <summary>
        /// This method tries to find the correct protocol policy for the given data.
        /// This process uses the registered bindings from an static dictionary, so we have the access to all available types.
        /// </summary>
        /// <param name="data">this should be the payload to parse</param>
        /// <returns>The found protocol policy or null if nothing was found</returns>
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

        /// <summary>
        /// This constructor calls a self registration to the bindings, if the type isn't exist. 
        /// </summary>
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

        /// <summary>
        /// Add a marker to the marker list of that protocol policy
        /// </summary>
        /// <param name="aByteSequence">matching sequence</param>
        /// <param name="aOffsetInStream">offset to the matching test</param>
        /// <param name="aEndMarker">true if it is of type end marker</param>
        /// <param name="aExclusiveMarker">true if the marker should be exclusive 
        /// (e.g. STX[inclusive]ETX   STX and ETX should be exclusive)</param>
        protected void AddMarker(IEnumerable<byte> aByteSequence, int aOffsetInStream, bool aEndMarker, bool aExclusiveMarker = false)
        {
            AddMarker(new Marker(aByteSequence, aOffsetInStream, aEndMarker, aExclusiveMarker));
        }

        /// <summary>
        /// Add a marker to the marker list of that protocol policy
        /// </summary>
        /// <param name="aMarker">the configured marker</param>
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

        /// <summary>
        /// Calculate the number of registered begin markers
        /// </summary>
        public int NumberOfBeginMarkers
        {
            get
            {
                return _markers.Count(x => !x.IsEndMarker);
            }
        }

        /// <summary>
        /// Sum all begin markers, to get the minimum begin marker length
        /// </summary>
        public int BeginMarkerLength
        {
            get
            {
                return _markers.Where(x => !x.IsEndMarker).Sum(x => x.SequenceLength);
            }
        }

        /// <summary>
        /// Get the first byte after the last begin marker
        /// </summary>
        public int BeginMarkerEnd
        {
            get
            {
                return _markers.Where(x => !x.IsEndMarker).Max(x => x.OffsetInStream + x.SequenceLength);
            }
        }

        /// <summary>
        /// Sum all end markers, to get the minimum end marker length
        /// </summary>
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

        /// <summary>
        /// Find all markers in the datagram
        /// </summary>
        /// <param name="data"></param>
        /// <param name="markers"></param>
        /// <returns></returns>
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

        /// <summary>
        /// remove data due to offsetNextValid 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offsetNextValid"></param>
        /// <returns></returns>
        private static int Eat(List<byte> data, int offsetNextValid)
        {
            var length = data.Count;
            if (offsetNextValid < 0)
            {
                data.Clear();
            }
            else
            {
                if (length <= offsetNextValid)
                    data.Clear();
                else
                    data.RemoveRange(0, offsetNextValid);
            }
            return length - data.Count;
        }

        /// <summary>
        /// inspect given data to extract messages
        /// </summary>
        /// <param name="data">incoming byte stream</param>
        /// <returns></returns>
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
                                        ready = true;
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
                                processingState = EProcessingState.Collecting;
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
                                var foundMarker = FindDatagramMarker(originData, _endMarkerSequences);
                                if (foundMarker != null && foundMarker.Result != null)
                                {
                                    lengthMarker = foundMarker.MetaData;
                                    if (foundMarker.Result.FullMatch)
                                        datagramLength = foundMarker.Result.MatchPos + lengthMarker.SequenceLength;
                                }
                            }
                            else
                                datagramLength = GetDatagramLength(originData);

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
            return messages;
        }

        /// <summary>
        /// Test if given data are from current protocol policy
        /// </summary>
        /// <param name="data">data to test</param>
        /// <returns>true if current protocol is matching</returns>
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

        #endregion
    }

}
