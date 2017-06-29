using Dacs7.Communication;
using Dacs7.Domain;
using Dacs7.Helper;
using Dacs7.Protocols;
using Dacs7.Protocols.RFC1006;
using Dacs7.Protocols.S7;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class Dacs7Client : IDacs7Client
    {
        #region Helper class
        private class CallbackHandler
        {
            public ushort Id { get; set; }
            public AutoResetEvent Event { get; set; }
            public IMessage ResponseMessage { get; set; }
            public Exception OccuredException { get; set; }
            public Action<IMessage> OnCallbackAction { get; set; }
        }
        #endregion

        #region Fields

        private readonly object _syncRoot = new object();
        private Exception _lastConnectException = null;
        private readonly AutoResetEvent _waitingForPlcConfiguration = new AutoResetEvent(false);
        private readonly UpperProtocolHandlerFactory _upperProtocolHandlerFactory = new UpperProtocolHandlerFactory();
        private readonly Queue<AutoResetEvent> _eventQueue = new Queue<AutoResetEvent>();
        private readonly ReaderWriterLockSlim _queueLockSlim = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _callbackLockSlim = new ReaderWriterLockSlim();
        private ushort _alarmUpdateId;
        private int _currentNumberOfPendingCalls;
        private int _sleeptimeAfterMaxPendingCallsReached;
        private ushort _maxParallelJobs;
        private ushort _maxParallelCalls;
        private TaskCreationOptions _taskCreationOptions = TaskCreationOptions.None;
        private ConnectionParameters _parameter;
        private ClientSocket _clientSocket;
        private readonly Dictionary<int, CallbackHandler> _callbacks = new Dictionary<int, CallbackHandler>();
        private string _connectionString = string.Empty;
        private int _timeout = 5000;
        private int _connectTimeout = 5000;
        private const ushort PduSizeDefault = 960;
        private int _referenceId;
        private readonly object _idLock = new object();
        private ushort _receivedPduSize;
        private readonly object _eventHandlerLock = new object();
        private event OnConnectionChangeEventHandler EventHandler;
        #endregion

        #region Properties
        public event OnConnectionChangeEventHandler OnConnectionChange
        {
            add
            {
                if (value != null)
                {
                    lock (_eventHandlerLock)
                        EventHandler += value;
                }
            }
            remove
            {
                if (value != null)
                {
                    lock (_eventHandlerLock)
                        EventHandler -= value;
                }
            }
        }

        public UInt16 PduSize
        {
            get { return _receivedPduSize <= 0 ? _parameter.GetParameter("PduSize", PduSizeDefault) : _receivedPduSize; }
            private set
            {
                if (value < _parameter.GetParameter("PduSize", PduSizeDefault))  //PDUSize is the maximum pdu size
                    _receivedPduSize = value;
            }
        }
        private UInt16 ItemReadSlice { get { return (UInt16)(PduSize - 18); } }  //18 Header and some other data 
        private UInt16 ItemWriteSlice { get { return (UInt16)(PduSize - 28); } } //28 Header and some other data

        /// <summary>
        /// Callback to an log Message Handler
        /// </summary>
        /// <returns></returns>
        public Action<string> OnLogEntry { get; set; }

        /// <summary>
        /// Max bytes to read in one telegram.
        /// </summary>
        /// <returns></returns>
        public UInt16 ReadItemMaxLength => ItemReadSlice;

        /// <summary>
        /// Max bytes to write in one telegram.
        /// </summary>
        /// <returns></returns>
        public UInt16 WriteItemMaxLength => ItemWriteSlice;

        /// <summary>
        /// Plc is connected or not.
        /// </summary>
        /// <returns></returns>
        public bool IsConnected { get { return _clientSocket != null && _clientSocket.IsConnected && !_clientSocket.Shutdown; } }

        /// <summary>
        /// Connection parameter for the plc connection separated by ';'. 
        /// Currently supported parameter:
        /// Data Source = [IP]:[PORT],[RACK],[SLOT];
        /// Connection Type = Pg; (Pg, Op, Basic)
        /// Receive Timeout = 5000;
        /// Reconnect = false;
        /// KeepAliveTime = 0u;
        /// KeepAliveInterval = 0u;
        /// PduSize = 480;
        /// </summary>
        /// <returns></returns>
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                if (value != _connectionString)
                {
                    _parameter = new ConnectionParameters(value);
                    _connectionString = value;
                }
            }
        }

        #endregion

        /// <summary>
        /// Finalizer to free the locks
        /// </summary>
        ~Dacs7Client()
        {
            if (_queueLockSlim != null)
            {
                try
                {
                    _queueLockSlim.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            if (_callbackLockSlim != null)
            {
                try
                {
                    _callbackLockSlim.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        /// <summary>
        /// Connect to a plc
        /// </summary>
        /// <param name="connectionString">This parameter overwrite the ConnectionString Property. Details see property connectionString.</param>
        public void Connect(string connectionString = null)
        {
            lock (_syncRoot)
            {
                if (!string.IsNullOrEmpty(connectionString))
                    ConnectionString = connectionString;

                if (!IsConnected)
                    Disconnect();

                AssignParameters();
                _clientSocket.OnConnectionStateChanged += OnClientStateChanged;
                _clientSocket.OnRawDataReceived += OnRawDataReceived;
                if (_clientSocket.Open())
                {
                    if (!_waitingForPlcConfiguration.WaitOne(_connectTimeout * 2))
                        throw new TimeoutException("Timeout while waiting for connection established signal");
                    if (_lastConnectException != null)
                        throw _lastConnectException;
                }
                else
                    throw new TimeoutException("Timeout while waiting for upper protocol");
            }
        }

        /// <summary>
        /// Connect to a plc asynchronous 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public Task ConnectAsync(string connectionString = null)
        {
            return Task.Factory.StartNew(() => Connect(connectionString), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Disconnect from the plc
        /// </summary>
        public void Disconnect()
        {
            _queueLockSlim.EnterWriteLock();
            try
            {
                while (_eventQueue.Any())
                {
                    try
                    {
                        var ev = _eventQueue.Dequeue();
                        ev.Set();
                        ev.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log(string.Format("Exception on Disconnect. Error was: {0}", ex.Message));
                    }
                }
            }
            finally
            {
                _queueLockSlim.ExitWriteLock();
            }


            lock (_syncRoot)
            {
                try
                {
                    if (_alarmUpdateId != 0)
                        UnregisterAlarmUpdate(_alarmUpdateId);
                    _clientSocket.Close();
                }
                catch (Exception ex)
                {
                    Log(string.Format("Exception on Disconnect while closing socket. Error was: {0}", ex.Message));
                }
            }
        }

        /// <summary>
        /// Disconnect from the plc asynchronous
        /// </summary>
        public Task DisconnectAsync()
        {
            return Task.Factory.StartNew(Disconnect, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Read data from the given data block number at the given offset.
        /// The length of the data will be extracted from the generic parameter.
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <returns>The read value or the default value of the type if the data could not be read.</returns>
        public T ReadAny<T>(int dbNumber, int offset)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            var t = typeof(T);
            var readType = t == typeof(bool) ? typeof(bool) : typeof(byte);
            var elementLength = t == typeof(bool) ? 1 : Marshal.SizeOf<T>();
            var data = ReadAny(PlcArea.DB, offset, readType, new[] { elementLength, dbNumber });
            if (data != null && data.Any())
                return (T)data.ConvertTo<T>();
            return default(T);
        }

        /// <summary>
        /// Read a number of items from the given generic type form the given data block number at the given offset and try convert it to the given dataType.
        /// </summary>
        /// <typeparam name="TElement">Element type of the resulting enumerable</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="numberOfItems">Number of items of the T to read. This could be the string length for a string, or the number of bytes/int for an array and so on. 
        /// The default value is always 1.</param>
        /// <returns>A list of TElement</returns>
        public IEnumerable<T> ReadAny<T>(int dbNumber, int offset, int numberOfItems)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            var t = typeof(T);
            var isBool = t == typeof(bool);
            var isString = t == typeof(string);
            var readType = (isBool && numberOfItems <= 1) ? typeof(bool) : typeof(byte);
            var elementLength = isBool ? (((numberOfItems + offset % 8) / 8) + 1) : (isString ? numberOfItems + 2 : Marshal.SizeOf<T>());
            var originOffset = offset;
            var bytesToRead = 1;

            if (numberOfItems >= 0)
            {
                if (isBool)
                {
                    offset /= 8;
                    var bitOffset = originOffset % 8;
                    bytesToRead = elementLength;
                }
                else if (isString)
                    bytesToRead = elementLength;
                else
                    bytesToRead = numberOfItems * elementLength;
            }

            var data = ReadAny(PlcArea.DB, offset, readType, new[] { bytesToRead, dbNumber });
            if (isString)
            {
                var result = new List<T>();
                string s = string.Empty;
                if (data.Length > 2)
                {
                    var length = (int)data[1];
                    s = new String(data.Skip(2).Select(x => Convert.ToChar(x)).ToArray()).Substring(0, length);
                }
                result.Add((T)Convert.ChangeType(s, t));
                return result;
            }
            else if (t != typeof(byte) && t != typeof(char) && numberOfItems > 0)
            {
                var result = new List<T>();
                var array = data as byte[];
                var bitOffset = originOffset % 8;
                for (int i = 0; i < Math.Max(numberOfItems, bytesToRead); i += elementLength)
                {
                    result.Add(isBool ?
                        (T)Convert.ChangeType(array.GetBit(bitOffset + i), t) :
                        (T)data.ConvertTo<T>(i));
                }
                return result;
            }
            return (IEnumerable<T>)data.ConvertTo<T>();
        }


        /// <summary>
        /// Read data from the PLC and return them as an array of byte.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> from which we want to read.</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="type">Specify the .Net data type for the read data, to determine the data size to read</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to read. If area is DB, second parameter is the db number.
        /// For example if you will read 500 bytes,  then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns>The read <see cref="T:byte[]"/></returns>
        public byte[] ReadAny(PlcArea area, int offset, Type type, params int[] args)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            var id = GetNextReferenceId();
            var length = Convert.ToUInt16(args.Any() ? args[0] : 0);
            var dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
            var policy = new S7JobReadProtocolPolicy();
            var packageLength = length;
            var readResult = new List<byte>();
            for (var j = 0; j < length; j += ItemReadSlice)
            {
                var readLength = Math.Min(ItemReadSlice, packageLength);
                var reqMsg = S7MessageCreator.CreateReadRequest(id, area, dbNr, offset + j, readLength, type);
                Log($"ReadAny: ProtocolDataUnitReference is {id}");

                if (PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                    if (errorClass == 0)
                    {
                        var items = cbh.ResponseMessage.GetAttribute("ItemCount", (byte)0);
                        if (items > 1)
                        {
                            var result = new List<object>();
                            for (var i = 0; i < items; i++)
                            {
                                var returnCode = cbh.ResponseMessage.GetAttribute($"Item[{i}].ItemReturnCode", (byte)0);
                                if (returnCode == 0xFF)
                                    result.Add(cbh.ResponseMessage.GetAttribute($"Item[{i}].ItemData", new byte[0]));
                                else
                                    throw new Dacs7ReturnCodeException(returnCode, i);
                            }
                            return result;
                        }
                        var firstReturnCode = cbh.ResponseMessage.GetAttribute("Item[0].ItemReturnCode", (byte)0);
                        if (firstReturnCode == 0xFF)
                            return cbh.ResponseMessage.GetAttribute("Item[0].ItemData", new byte[0]);
                        throw new Dacs7ContentException(firstReturnCode, 0);
                    }
                    var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                    throw new Dacs7Exception(errorClass, errorCode);
                }) is byte[] currentData)
                    readResult.AddRange(currentData);
                else
                    throw new InvalidDataException("Returned data are null");
                packageLength -= ItemReadSlice;
            }
            return readResult.ToArray<byte>();
        }

        /// <summary>
        /// Read multiple variables with one call from the PLC and return them as a list of the correct read types.
        /// </summary>
        /// <param name="parameters">A list of <see cref="ReadOperationParameter"/>, so multiple read requests can be handled in one message</param>
        /// <returns>A list of <see cref="T:object"/> where every list entry contains the read value in order of the given parameter order</returns>
        public IEnumerable<byte[]> ReadAnyRaw(IEnumerable<ReadOperationParameter> parameters)
        {
            var id = GetNextReferenceId();
            var policy = new S7JobReadProtocolPolicy();
            var readResult = new List<byte[]>();

            foreach (var part in GetOperationParts(parameters))
            {
                var reqMsg = S7MessageCreator.CreateReadRequests(id, part);

                //check the created message size!
                var currentPackageSize = reqMsg.GetAttribute("ParamLength", (ushort)0) + reqMsg.GetAttribute("DataLength", (ushort)0);
                if (PduSize < currentPackageSize)
                    throw new Dacs7ToMuchDataPerCallException(ItemReadSlice, currentPackageSize);

                Log(string.Format("ReadAny: ProtocolDataUnitReference is {0}", id));

                if (PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                    if (errorClass == 0)
                    {
                        var items = cbh.ResponseMessage.GetAttribute("ItemCount", (byte)0);
                        if (items > 1)
                        {
                            var result = new List<byte[]>();
                            for (var i = 0; i < items; i++)
                            {
                                var item = new List<byte>();
                                var returnCode = cbh.ResponseMessage.GetAttribute(string.Format("Item[{0}].ItemReturnCode", i), (byte)0);
                                if (returnCode == 0xFF)
                                    result.Add(cbh.ResponseMessage.GetAttribute(string.Format("Item[{0}].ItemData", i), new byte[0]));
                                else
                                    throw new Dacs7ReturnCodeException(returnCode, i);
                            }
                            return result;
                        }
                        var firstReturnCode = cbh.ResponseMessage.GetAttribute("Item[0].ItemReturnCode", (byte)0);
                        if (firstReturnCode == 0xFF)
                            return new List<byte[]> { cbh.ResponseMessage.GetAttribute("Item[0].ItemData", new byte[0]) };
                        throw new Dacs7ContentException(firstReturnCode, 0);
                    }
                    var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                    throw new Dacs7Exception(errorClass, errorCode);
                }) is List<byte[]> currentData)
                    readResult.AddRange(currentData);
                else
                    throw new InvalidDataException("Returned data are null");
            }
            if (readResult == null || !readResult.Any())
                throw new InvalidDataException("Returned data are null");
            return readResult;
        }

        /// <summary>
        /// Read multiple variables with one call from the PLC and return them as a list of the correct read types.
        /// </summary>
        /// <param name="parameters">A list of <see cref="ReadOperationParameter"/>, so multiple read requests can be handled in one message</param>
        /// <returns>A list of <see cref="T:object"/> where every list entry contains the read value in order of the given parameter order</returns>
        public IEnumerable<object> ReadAny(IEnumerable<ReadOperationParameter> parameters)
        {
            var readResult = ReadAnyRaw(parameters).ToArray();
            var resultList = new List<object>();
            int current = 0;
            foreach (var param in parameters)
            {
                var data = readResult[current++];
                var t = param.Type;
                var isBool = t == typeof(bool);
                var isString = t == typeof(string);
                var readType = typeof(byte);
                var numberOfItems = param.Args != null && param.Args.Length > 0 ? param.Args[0] : 1;
#pragma warning disable CS0618 // Type or member is obsolete
                var elementLength = isBool ? 1 : (isString ? numberOfItems + 2 : Marshal.SizeOf(t));  //Will be supported: https://github.com/dotnet/corefx/pull/10541
#pragma warning restore CS0618 // Type or member is obsolete
                var originOffset = param.Offset;


                if (isString)
                {
                    string s = string.Empty;
                    if (data.Length > 2)
                    {
                        var length = (int)data[1];
                        if (length > data.Length - 2)
                            s = string.Empty; // INVALID DATA
                        else
                            s = new String(data.Skip(2).Select(x => Convert.ToChar(x)).ToArray()).Substring(0, length);
                    }
                    resultList.Add(Convert.ChangeType(s, t));
                }
                else if (t != typeof(byte) && t != typeof(char) && numberOfItems > 1)
                {
                    var result = new List<object>();
                    var array = data as byte[];
                    var byteOffet = 0;
                    var bitOffset = originOffset % 8;
                    for (int i = 0; i < numberOfItems; i += elementLength)
                    {
                        result.Add(isBool ?
                            Convert.ChangeType(array[byteOffet].GetBit(bitOffset + i), t) :
                            data.ConvertTo(t, i));
                    }
                    resultList.Add(result);
                }
                else if (t == typeof(byte) || t == typeof(char))
                {
                    resultList.Add(Convert.ChangeType(data[0], t));
                }
                else
                    resultList.Add(data.ConvertTo(t));
            }
            return resultList;
        }


        /// <summary>
        /// Read data from the plc by using Tasks.
        /// </summary>
        /// <param name="area">Specify the plc area to read.  e.g. IB InputByte</param>
        /// <param name="offset">Specify the read offset</param>
        /// <param name="type">Specify the .Net data type for the red data</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        /// http://www.tugberkugurlu.com/archive/how-and-where-concurrent-asynchronous-io-with-asp-net-web-api
        private byte[] ReadAnyPartsAsync(PlcArea area, int offset, Type type, params int[] args)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            try
            {
                var length = Convert.ToUInt16(args.Any() ? args[0] : 0);
                var dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
                var packageLength = length;
                var readResult = new List<byte>();
                var requests = new List<Task<byte[]>>();
                for (var j = 0; j < length; j += ItemReadSlice)
                {
                    while (_currentNumberOfPendingCalls >= _maxParallelCalls)
                        Thread.Sleep(_sleeptimeAfterMaxPendingCallsReached);
                    var readLength = Math.Min(ItemReadSlice, packageLength);
                    var readOffset = offset + j;
                    requests.Add(Task.Factory.StartNew(() => ReadAny(area, readOffset, type, new int[] { readLength, dbNr }), _taskCreationOptions));
                    packageLength -= ItemReadSlice;
                }

                Task.WaitAll(requests.ToArray());

                foreach (var result in requests.Select(request => request.Result as byte[]))
                {
                    if (result != null)
                        readResult.AddRange(result);
                    else
                        throw new InvalidDataException("Returned data are null");
                }
                return readResult.ToArray<byte>();
            }
            catch (AggregateException exception)
            {
                //Throw only the first exception
                throw exception.InnerExceptions.First();
            }
        }

        /// <summary>
        /// Read data from the PLC as parallel. The size of a message to and from the PLC is limited by the PDU-Size. This method splits the message
        /// and recombine it after receiving them. And this is done in parallel.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> from which we want to read.</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="type">Specify the .Net data type for the read data. This parameter is used to determine the correct data length we have to read</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to read. If area is DB, second parameter is the db number.
        /// For example if you will read 500 bytes,  then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns>read <see cref="T:byte[]"/></returns>
        public object ReadAnyParallel(PlcArea area, int offset, Type type, params int[] args)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            try
            {
                var length = Convert.ToUInt16(args.Any() ? args[0] : 0);
                var dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
                var packageLength = length;
                var readResult = new List<byte>();
                var results = new Dictionary<int, byte[]>();
                var requests = new List<Tuple<int, int, int>>();
                for (var j = 0; j < length; j += ItemReadSlice)
                {
                    var readLength = Math.Min(ItemReadSlice, packageLength);
                    requests.Add(new Tuple<int, int, int>(j, offset + j, readLength));
                    packageLength -= ItemReadSlice;
                }

                foreach (var result in requests.AsParallel().Select(result => new KeyValuePair<int, byte[]>(result.Item1, ReadAny(area, result.Item2, type, new int[] { result.Item3, dbNr }) as byte[])))
                {
                    if (result.Value != null)
                        results.Add(result.Key, result.Value);
                    else
                        throw new InvalidDataException("Returned data are null");
                }

                foreach (var result in results.OrderBy(x => x.Key).Select(x => x.Value))
                    readResult.AddRange(result);

                return readResult.ToArray<byte>();
            }
            catch (AggregateException exception)
            {
                //Throw only the first exception
                throw exception.InnerExceptions.First();
            }
        }

        /// <summary>
        /// Read data from the PLC asynchronous and convert it to the given .Net type.
        /// This method wraps the call in a task.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> from which we want to read.</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="type">Specify the .Net data type for the read data</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to read. If area is DB, second parameter is the db number.
        /// For example if you will read 500 bytes,  then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns></returns>
        public Task<byte[]> ReadAnyAsync(PlcArea area, int offset, Type type, params int[] args)
        {
            return Task.Factory.StartNew(() =>
                _maxParallelCalls <= 1 ?
                ReadAny(area, offset, type, args) :
                ReadAnyPartsAsync(area, offset, type, args),
                _taskCreationOptions);
        }

        /// <summary>
        /// Write data to the given PLC area.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="length">the length of data in bytes you want to write  e.g. if you have a value byte[500] and you want to write
        /// only the first 100 bytes, you have to set length to 100. If length is not set, the correct size will be determined by the value size.</param>
        public void WriteAny<T>(PlcArea area, int offset, T value, int length = -1)
        {
            if (area == PlcArea.DB)
                throw new ArgumentException("The argument area could not be DB.");
            var size = CalculateSizeForGenericWriteOperation<T>(area, value, length, out Type elementType);
            WriteAny(area, offset, value, new int[] { size });
        }

        /// <summary>
        /// Write data to the given PLC data block with offset and length.
        /// </summary>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="length">the length of data in bytes you want to write  e.g. if you have a value byte[500] and you want to write
        /// only the first 100 bytes, you have to set length to 100. If length is not set, the correct size will be determined by the value size.</param>
        public void WriteAny<T>(int dbNumber, int offset, T value, int length = -1)
        {
            var size = CalculateSizeForGenericWriteOperation(PlcArea.DB, value, length, out Type elementType);
            if (elementType == typeof(bool))
            {
                //with bool's we have to create a multi write request
                WriteAny((value as IEnumerable<bool>).Select((element, i) => WriteOperationParameter.Create(dbNumber, offset + i, element)));
                return;
            }
            WriteAny(PlcArea.DB, offset, value, new int[] { size, dbNumber });
        }

        /// <summary>
        /// Write data to the given PLC area.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to write. If area is DB, second parameter is the db number.
        /// For example if you will write 500 bytes, then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns></returns>
        public void WriteAny(PlcArea area, int offset, object value, params int[] args)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();

            var id = GetNextReferenceId();
            var length = Convert.ToUInt16(args.Any() ? args[0] : 0);
            var dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
            var policy = new S7JobWriteProtocolPolicy();
            var packageLength = length;
            var isToExtract = length > ItemWriteSlice;
            for (var j = 0; j < length; j += ItemWriteSlice)
            {
                var writeLength = Math.Min(ItemWriteSlice, packageLength);
                var reqMsg = S7MessageCreator.CreateWriteRequest(id, area, dbNr, offset + j, writeLength, isToExtract ? ExtractData(value, j, writeLength) : value);
                Log($"WriteAny: ProtocolDataUnitReference is {id}");
                PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                    if (errorClass == 0x00)
                    {
                        var items = cbh.ResponseMessage.GetAttribute("ItemCount", (byte)0);
                        for (var i = 0; i < items; i++)
                        {
                            var returnCode = cbh.ResponseMessage.GetAttribute($"Item[{i}].ItemReturnCode", (byte)0);
                            if (returnCode != 0xff)
                                throw new Dacs7ContentException(returnCode, i);
                        }
                        //all write operations are successfully
                        return;
                    }
                    var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                    throw new Dacs7Exception(errorClass, errorCode);
                });
                packageLength -= ItemWriteSlice;
            }
        }

        /// <summary>
        /// Write data parallel to the given PLC area.The size of a message to and from the PLC is limited by the PDU-Size. This method splits the message
        /// and recombine it after receiving them. And this is done in parallel.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to write. If area is DB, second parameter is the db number.
        /// For example if you will write 500 bytes, then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns></returns>
        public void WriteAnyParallel(PlcArea area, int offset, object value, params int[] args)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            try
            {
                var length = Convert.ToUInt16(args.Any() ? args[0] : 0);
                var dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
                var packageLength = length;
                var requests = new List<Tuple<int, int, int>>();
                var isToExtract = length > ItemWriteSlice;
                for (var j = 0; j < length; j += ItemReadSlice)
                {
                    var writeLength = Math.Min(ItemReadSlice, packageLength);
                    requests.Add(new Tuple<int, int, int>(j, offset + j, writeLength));
                    packageLength -= ItemReadSlice;
                }

                Parallel.ForEach(requests, (request) => WriteAny(area, request.Item2, isToExtract ? ExtractData(value, request.Item1, request.Item3) : value, new int[] { request.Item3, dbNr }));
            }
            catch (AggregateException exception)
            {
                //Throw only the first exception
                throw exception.InnerExceptions.First();
            }
        }


        internal static int CalculateSizeForGenericWriteOperation<T>(PlcArea area, T value, int length, out Type elementType)
        {
            elementType = null;
            if (value is Array && length < 0)
            {
                elementType = typeof(T).GetElementType();
                length = (value as Array).Length * TransportSizeHelper.DataTypeToSizeByte(elementType, area);
            }
            var size = length < 0 ? TransportSizeHelper.DataTypeToSizeByte(typeof(T), PlcArea.DB) : length;
            var stringValue = value as string;
            if (stringValue != null)
            {
                if (length < 0) size = stringValue.Length;
                size += 2;
            }
            return size;
        }


        /// <summary>
        /// Write data parallel to the connected plc.
        /// </summary>
        /// <param name="area">Specify the plc area to write to.  e.g. OB OutputByte</param>
        /// <param name="offset">Specify the write offset</param>
        /// <param name="value">Value to write</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        private void WriteAnyPartsAsync(PlcArea area, int offset, object value, params int[] args)
        {
            try
            {
                var length = Convert.ToUInt16(args.Any() ? args[0] : 0);
                var dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
                var packageLength = length;
                var requests = new List<Task>();
                var isToExtract = length > ItemWriteSlice;
                for (var j = 0; j < length; j += ItemReadSlice)
                {
                    while (_currentNumberOfPendingCalls >= _maxParallelCalls)
                        Thread.Sleep(_sleeptimeAfterMaxPendingCallsReached);
                    var writeLength = Math.Min(ItemReadSlice, packageLength);
                    var writeOffset = offset + j;
                    var data = isToExtract ? ExtractData(value, j, writeLength) : value;
                    requests.Add(Task.Factory.StartNew(() => WriteAny(area, writeOffset, data, new int[] { writeLength, dbNr }), TaskCreationOptions.LongRunning));
                    packageLength -= ItemReadSlice;
                }

                Task.WaitAll(requests.ToArray());
            }
            catch (AggregateException exception)
            {
                //Throw only the first exception
                throw exception.InnerExceptions.First();
            }
        }


        /// <summary>
        /// Write data asynchronous to the given PLC area.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">>Should be the value you want to write.</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to write. If area is DB, second parameter is the db number.
        /// For example if you will write 500 bytes, then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns><see cref="Task"/></returns>
        public Task WriteAnyAsync(PlcArea area, int offset, object value, params int[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                if (_maxParallelCalls <= 1)
                    WriteAny(area, offset, value, args);
                else
                    WriteAnyPartsAsync(area, offset, value, args);
            },
            _taskCreationOptions);

        }


        /// <summary>
        /// Write multiple variables with one call to the PLC.
        /// </summary>
        /// <param name="parameters">A list of <see cref="WriteOperationParameter"/>, so multiple write requests can be handled in one message</param>
        public void WriteAny(IEnumerable<WriteOperationParameter> parameters)
        {
            var id = GetNextReferenceId();
            var policy = new S7JobWriteProtocolPolicy();
            foreach (var item in GetOperationParts(parameters))
            {
                var reqMsg = S7MessageCreator.CreateWriteRequests(id, item);

                //check the created message size!
                var currentPackageSize = reqMsg.GetAttribute("ParamLength", (ushort)0) + reqMsg.GetAttribute("DataLength", (ushort)0);
                if (PduSize < currentPackageSize)
                    throw new Dacs7ToMuchDataPerCallException(ItemReadSlice, currentPackageSize);


                Log(string.Format("WriteAny: ProtocolDataUnitReference is {0}", id));
                PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                    if (errorClass == 0x00)
                    {
                        var items = cbh.ResponseMessage.GetAttribute("ItemCount", (byte)0);
                        for (var i = 0; i < items; i++)
                        {
                            var returnCode = cbh.ResponseMessage.GetAttribute(string.Format("Item[{0}].ItemReturnCode", i), (byte)0);
                            if (returnCode != 0xff)
                                throw new Dacs7ContentException(returnCode, i);
                        }
                    //all write operations are successfully
                    return;
                    }
                    var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                    throw new Dacs7Exception(errorClass, errorCode);
                });
            }
        }


        /// <summary>
        /// Read the number of blocks in the PLC per type
        /// </summary>
        /// <returns><see cref="IPlcBlocksCount"/> where you have access to the count of all the block types.</returns>
        public IPlcBlocksCount GetBlocksCount()
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateBlocksCountRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            Log($"GetBlocksCount: ProtocolDataUnitReference is {id}");
            return PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                var errorCode = cbh.ResponseMessage.GetAttribute("ParamErrorCode", (ushort)0);
                if (errorCode == 0)
                {
                    var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
                    if (returnCode == 0xff)
                    {
                        var sslData = cbh.ResponseMessage.GetAttribute("SSLData", new byte[0]);
                        if (sslData.Any())
                        {
                            var bc = new PlcBlocksCount();
                            for (var i = 0; i < sslData.Length; i += 4)
                            {
                                if (sslData[i] == 0x30)
                                {
                                    var type = (PlcBlockType)sslData[i + 1];
                                    var value = sslData.GetSwap<UInt16>(i + 2);

                                    switch (type)
                                    {
                                        case PlcBlockType.Ob:
                                            bc.Ob = value;
                                            break;
                                        case PlcBlockType.Fb:
                                            bc.Fb = value;
                                            break;
                                        case PlcBlockType.Fc:
                                            bc.Fc = value;
                                            break;
                                        case PlcBlockType.Db:
                                            bc.Db = value;
                                            break;
                                        case PlcBlockType.Sdb:
                                            bc.Sdb = value;
                                            break;
                                        case PlcBlockType.Sfc:
                                            bc.Sfc = value;
                                            break;
                                        case PlcBlockType.Sfb:
                                            bc.Sfb = value;
                                            break;
                                    }
                                }
                            }
                            return bc;

                        }
                        throw new InvalidDataException("SSL Data are empty!");
                    }
                    throw new Dacs7ReturnCodeException(returnCode);
                }
                throw new Dacs7ParameterException(errorCode);
            }) as IPlcBlocksCount;
        }

        /// <summary>
        /// Read the number of blocks in the PLC per type asynchronous. This means the call is wrapped in a Task.
        /// </summary>
        /// <returns><see cref="IPlcBlocksCount"/> where you have access to the count of all the block types.</returns>
        public Task<IPlcBlocksCount> GetBlocksCountAsync()
        {
            return Task.Factory.StartNew(() => GetBlocksCount(), _taskCreationOptions);
        }

        /// <summary>
        /// Get all blocks of the specified type.
        /// </summary>
        /// <param name="type">Block type to read. <see cref="PlcBlockType"/></param>
        /// <returns>Return a list off all blocks <see cref="IPlcBlock"/> of this type</returns>
        public IEnumerable<IPlcBlocks> GetBlocksOfType(PlcBlockType type)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            var id = GetNextReferenceId();
            var policy = new S7UserDataProtocolPolicy();
            var blocks = new List<IPlcBlocks>();
            var lastUnit = false;
            var sequenceNumber = (byte)0x00;
            Log($"GetBlocksOfType: ProtocolDataUnitReference is {id}");

            do
            {
                var reqMsg = S7MessageCreator.CreateBlocksOfTypeRequest(id, type, sequenceNumber);

                if (PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    var errorCode = cbh.ResponseMessage.GetAttribute("ParamErrorCode", (ushort)0);
                    if (errorCode == 0)
                    {
                        var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
                        if (returnCode == 0xff)
                        {
                            var sslData = cbh.ResponseMessage.GetAttribute("SSLData", new byte[0]);
                            if (sslData.Any())
                            {
                                var result = new List<IPlcBlocks>();
                                for (var i = 0; i < sslData.Length; i += 4)
                                {
                                    result.Add(new PlcBlocks
                                    {
                                        Number = sslData.GetSwap<ushort>(i),
                                        Flags = sslData[i + 2],
                                        Language = PlcBlockInfo.GetLanguage(sslData[i + 3])
                                    });
                                }

                                lastUnit = cbh.ResponseMessage.GetAttribute("LastDataUnit", true);
                                sequenceNumber = cbh.ResponseMessage.GetAttribute("SequenceNumber", (byte)0x00);

                                return result;
                            }
                            throw new InvalidDataException("SSL Data are empty!");

                        }
                        throw new Dacs7ReturnCodeException(returnCode);
                    }
                    throw new Dacs7ParameterException(errorCode);
                }) is IEnumerable<IPlcBlocks> blocksPart)
                    blocks.AddRange(blocksPart);
            } while (!lastUnit);
            return blocks;
        }

        /// <summary>
        /// Get all blocks of the specified type asynchronous.This means the call is wrapped in a Task.
        /// </summary>
        /// <param name="type">Block type to read. <see cref="PlcBlockType"/></param>
        /// <returns>Return a list off all blocks <see cref="IPlcBlock"/> of this type</returns>
        public Task<IEnumerable<IPlcBlocks>> GetBlocksOfTypeAsync(PlcBlockType type)
        {
            return Task.Factory.StartNew(() => GetBlocksOfType(type), _taskCreationOptions);
        }

        /// <summary>
        /// Read the meta data of a block from the PLC.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB   <see cref="PlcBlockType"/></param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns><see cref="IPlcBlockInfo"/> where you have access tho the detailed meta data of the block.</returns>
        public IPlcBlockInfo ReadBlockInfo(PlcBlockType blockType, int blocknumber)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateBlockInfoRequest(id, blockType, blocknumber);
            var policy = new S7UserDataProtocolPolicy();
            Log($"ReadBlockInfo: ProtocolDataUnitReference is {id}");
            return PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                var errorCode = cbh.ResponseMessage.GetAttribute("ParamErrorCode", (ushort)0);
                if (errorCode == 0)
                {
                    var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
                    if (returnCode == 0xff)
                    {
                        var sslData = cbh.ResponseMessage.GetAttribute("SSLData", new byte[0]);
                        if (sslData.Any())
                        {
                            var datalength = sslData[3];
                            var authorOffset = sslData[5];

                            return new PlcBlockInfo()
                            {
                                BlockLanguage = PlcBlockInfo.GetLanguage(sslData[10]),
                                BlockType = PlcBlockInfo.GetPlcBlockType(sslData[11]),
                                BlockNumber = PlcBlockInfo.GetU16(sslData, 12),
                                Length = PlcBlockInfo.GetU32(sslData, 14),
                                Password = PlcBlockInfo.GetString(18, 4, sslData),
                                LastCodeChange = PlcBlockInfo.GetDt(sslData[22], sslData[23], sslData[24], sslData[25], sslData[26], sslData[27]),
                                LastInterfaceChange = PlcBlockInfo.GetDt(sslData[28], sslData[29], sslData[30], sslData[31], sslData[32], sslData[33]),

                                LocalDataSize = PlcBlockInfo.GetU16(sslData, 38),
                                CodeSize = PlcBlockInfo.GetU16(sslData, 40),

                                Author = PlcBlockInfo.GetString(42 + authorOffset, 8, sslData),
                                Family = PlcBlockInfo.GetString(42 + 8 + authorOffset, 8, sslData),
                                Name = PlcBlockInfo.GetString(42 + 16 + authorOffset, 8, sslData),
                                VersionHeader = PlcBlockInfo.GetVersion(sslData[42 + 24 + authorOffset]),
                                Checksum = PlcBlockInfo.GetCheckSum(42 + 26 + authorOffset, sslData)
                            };
                        }
                        throw new InvalidDataException("SSL Data are empty!");
                    }
                    throw new Dacs7ReturnCodeException(returnCode);
                }
                throw new Dacs7ParameterException(errorCode);
            }) as IPlcBlockInfo;
        }

        /// <summary>
        /// Read the full data of a block from the PLC.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB  <see cref="PlcBlockType"/></param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns>returns the see  <see cref="T:byte[]"/> of the block.</returns>
        public byte[] UploadPlcBlock(PlcBlockType blockType, int blocknumber)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            var id = GetNextReferenceId();
            var policy = new S7JobUploadProtocolPolicy();
            Log("ReadBlockInfo: ProtocolDataUnitReference is {id}");

            //Start Upload
            var reqMsg = S7MessageCreator.CreateStartUploadRequest(id, blockType, blocknumber);
            uint controlId = 0;
            PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                if (errorClass == 0x00)
                {
                    var function = cbh.ResponseMessage.GetAttribute("Function", (byte)0);
                    if (function == 0x1d)
                    {
                        //all write operations are successfully
                        var paramData = cbh.ResponseMessage.GetAttribute("ParameterData", new byte[0]);
                        if (paramData.Length >= 7)
                            controlId = paramData.GetSwap<uint>(3);
                        return;
                    }

                }
                var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                throw new Dacs7Exception(errorClass, errorCode);
            });

            //Upload packages
            reqMsg = S7MessageCreator.CreateUploadRequest(id, blockType, blocknumber, controlId);
            var data = new List<byte>();
            var hasNext = false;
            do
            {
                hasNext = false;
                PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                    if (errorClass == 0x00)
                    {
                        var function = cbh.ResponseMessage.GetAttribute("Function", (byte)0);
                        if (function == 0x1e)
                        {
                            //all write operations are successfully
                            var paramdata = cbh.ResponseMessage.GetAttribute("ParameterData", new byte[0]);
                            if (paramdata.Length == 1)
                                hasNext = paramdata[0] == 0x01;
                            var currentdata = cbh.ResponseMessage.GetAttribute("Data", new byte[0]).Skip(3);
                            data.AddRange(currentdata);
                            return;
                        }
                    }
                    var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                    throw new Dacs7Exception(errorClass, errorCode);
                });
            } while (hasNext);


            reqMsg = S7MessageCreator.CreateEndUploadRequest(id, blockType, blocknumber, controlId);
            PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                if (errorClass == 0x00)
                {
                    var function = cbh.ResponseMessage.GetAttribute("Function", (byte)0);
                    if (function == 0x1f)
                    {
                        //all write operations are successfully
                        return;
                    }
                }
                var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                throw new Dacs7Exception(errorClass, errorCode);
            });

            return data.ToArray();
        }

        ///// <summary>
        ///// Write the full data of a block to the plc.
        ///// </summary>
        ///// <param name="blockType">Specify the block type to read. e.g. DB</param>
        ///// <param name="blocknumber">Specify the Number of the block</param>
        ///// <param name="data">Plc block in byte</param>
        ///// <returns></returns>
        //public bool DownloadPlcBlock(PlcBlockType blockType, int blocknumber, byte[] data)
        //{
        //    //TODO: Implement it correct
        //    throw new NotImplementedException();

        //    if (!IsConnected)
        //        throw new Dacs7NotConnectedException();
        //    var id = GetNextReferenceId();
        //    var reqMsg = S7MessageCreator.CreateStartDownloadRequest(id, blockType, blocknumber, data);  //Start Download
        //    var policy = new S7UserDataProtocolPolicy();
        //    Log($"DownloadPlcBlock: ProtocolDataUnitReference is {id}");
        //    return (bool)PerformeDataExchange(id, reqMsg, policy, (cbh) =>
        //    {
        //        var errorCode = cbh.ResponseMessage.GetAttribute("ParamErrorCode", (ushort)0xff);
        //        if (errorCode == 0)
        //        {
        //            var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
        //            if (returnCode == 0x00)
        //            {

        //                //TODO!!!!!!!!!!!!!!!!!!!!!!
        //                var callbackId = GetNextReferenceId();
        //                var cbhOnUpdate = GetCallbackHandler(callbackId, true);
        //                //_alarmUpdateId = callbackId;
        //                cbhOnUpdate.OnCallbackAction = (msg) =>
        //                {
        //                    if (msg != null)
        //                    {
        //                        try
        //                        {
        //                            var returnCodeCb = msg.GetAttribute("ReturnCode", (byte)0);
        //                            if (returnCodeCb == 0xff)
        //                            {
        //                                var dataLength = msg.GetAttribute("UserDataLength", (UInt16)0);
        //                                if (dataLength > 0)
        //                                {
        //                                    var subItemName = string.Format("Alarm[{0}].", 0) + "{0}";
        //                                    var isComing = msg.GetAttribute(string.Format(subItemName, "IsComing"), false);
        //                                    return;
        //                                }
        //                                throw new InvalidDataException("SSL Data are empty!");
        //                            }
        //                            throw new Dacs7ReturnCodeException(returnCodeCb);
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Log(ex.Message);
        //                        }
        //                    }
        //                    else if (cbhOnUpdate.OccuredException != null)
        //                    {
        //                        Log(cbhOnUpdate.OccuredException.Message);
        //                    }
        //                };
        //                return callbackId;
        //            }
        //            throw new Dacs7ReturnCodeException(returnCode);
        //        }
        //        throw new Dacs7ParameterException(errorCode);
        //    });
        //}

        /// <summary>
        /// Read the meta data of a block asynchronous from the PLC.This means the call is wrapped in a Task.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB  <see cref="PlcBlockType"/></param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns>a <see cref="Task"/> of <see cref="IPlcBlockInfo"/> where you have access tho the detailed meta data of the block.</returns>
        public Task<IPlcBlockInfo> ReadBlockInfoAsync(PlcBlockType blockType, int blocknumber)
        {
            return Task.Factory.StartNew(() => ReadBlockInfo(blockType, blocknumber), _taskCreationOptions);
        }

        /// <summary>
        /// Read the current pending alarms from the PLC.
        /// </summary>
        /// <returns>returns a list of all pending alarms</returns>
        public IEnumerable<IPlcAlarm> ReadPendingAlarms()
        {
            var id = GetNextReferenceId();
            var policy = new S7UserDataProtocolPolicy();
            var alarms = new List<IPlcAlarm>();
            var lastUnit = false;
            var sequenceNumber = (byte)0x00;
            Log($"ReadBlockInfo: ProtocolDataUnitReference is {id}");

            do
            {
                var reqMsg = S7MessageCreator.CreatePendingAlarmRequest(id, sequenceNumber);

                if (PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    var errorCode = cbh.ResponseMessage.GetAttribute("ParamErrorCode", (ushort)0);
                    if (errorCode == 0)
                    {
                        var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
                        if (returnCode == 0xff)
                        {
                            var numberOfAlarms = cbh.ResponseMessage.GetAttribute("NumberOfAlarms", 0);
                            var result = new List<IPlcAlarm>();
                            for (var i = 0; i < numberOfAlarms; i++)
                            {
                                var subItemName = $"Alarm[{i}]." + "{0}";
                                var isComing = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "IsComing"), false);
                                var isAck = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "IsAck"), false);
                                var ack = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "Ack"), false);
                                result.Add(new PlcAlarm
                                {
                                    Id = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "Id"), (ushort)0),
                                    MsgNumber = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "MsgNumber"), (uint)0),
                                    IsComing = isComing,
                                    IsAck = isAck,
                                    Ack = ack,
                                    AlarmSource = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "AlarmSource"), (ushort)0),
                                    Timestamp = ExtractTimestamp(cbh.ResponseMessage, i, !isComing && !isAck && ack ? 1 : 0),
                                    AssotiatedValue = ExtractAssotiatedValue(cbh.ResponseMessage, i)
                                });
                            }

                            lastUnit = cbh.ResponseMessage.GetAttribute("LastDataUnit", true);
                            sequenceNumber = cbh.ResponseMessage.GetAttribute("SequenceNumber", (byte)0x00);

                            return result;
                        }
                        throw new Dacs7ReturnCodeException(returnCode);
                    }
                    throw new Dacs7ParameterException(errorCode);
                }) is IEnumerable<IPlcAlarm> alarmPart)
                    alarms.AddRange(alarmPart);
            } while (!lastUnit);
            return alarms;
        }

        /// <summary>
        /// Read the current pending alarms asynchronous from the PLC.
        /// </summary>
        /// <returns>returns a list of all pending alarms</returns>
        public Task<IEnumerable<IPlcAlarm>> ReadPendingAlarmsAsync()
        {
            return Task.Factory.StartNew(() => ReadPendingAlarms(), _taskCreationOptions);
        }

        /// <summary>
        /// Register a alarm changed callback. After this you will be notified if a Alarm is coming or going.
        /// </summary>
        /// <param name="onAlarmUpdate">Callback to alarm data change</param>
        /// <param name="onErrorOccured">Callback to error in routine</param>
        /// <returns></returns>
        public ushort RegisterAlarmUpdateCallback(Action<IPlcAlarm> onAlarmUpdate, Action<Exception> onErrorOccured = null)
        {
            if (_alarmUpdateId != 0)
                throw new Exception("There is already an update callback registered. Only one alarm update callback is allowed!");

            if (!IsConnected)
                throw new Dacs7NotConnectedException();

            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateAlarmCallbackRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            Log($"RegisterAlarmUpdateCallback: ProtocolDataUnitReference is {id}");
            return (ushort)PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                var errorCode = cbh.ResponseMessage.GetAttribute("ParamErrorCode", (ushort)0xff);
                if (errorCode == 0)
                {
                    var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
                    if (returnCode == 0xff)
                    {
                        var callbackId = GetNextReferenceId();
                        var cbhOnUpdate = GetCallbackHandler(callbackId, true);
                        _alarmUpdateId = callbackId;
                        cbhOnUpdate.OnCallbackAction = (msg) =>
                        {
                            if (msg != null)
                            {
                                try
                                {
                                    var returnCodeCb = msg.GetAttribute("ReturnCode", (byte)0);
                                    if (returnCodeCb == 0xff)
                                    {
                                        var dataLength = msg.GetAttribute("UserDataLength", (UInt16)0);
                                        if (dataLength > 0)
                                        {
                                            var subItemName = "Alarm[0].{0}";
                                            var isComing = msg.GetAttribute(string.Format(subItemName, "IsComing"), false);
                                            onAlarmUpdate(new PlcAlarm
                                            {
                                                Id = msg.GetAttribute(string.Format(subItemName, "Id"), (ushort)0),
                                                MsgNumber = msg.GetAttribute(string.Format(subItemName, "MsgNumber"), (uint)0),
                                                IsComing = isComing,
                                                IsAck = msg.GetAttribute(string.Format(subItemName, "IsAck"), false),
                                                Ack = msg.GetAttribute(string.Format(subItemName, "Ack"), false),
                                                AlarmSource = msg.GetAttribute(string.Format(subItemName, "AlarmSource"), (ushort)0),
                                                Timestamp = ExtractTimestamp(msg, 0),
                                                AssotiatedValue = ExtractAssotiatedValue(msg, 0)
                                            });
                                            return;
                                        }
                                        throw new InvalidDataException("SSL Data are empty!");
                                    }
                                    throw new Dacs7ReturnCodeException(returnCodeCb);
                                }
                                catch (Exception ex)
                                {
                                    Log(ex.Message);
                                    onErrorOccured?.Invoke(ex);
                                }
                            }
                            else if (cbhOnUpdate.OccuredException != null)
                            {
                                Log(cbhOnUpdate.OccuredException.Message);
                                onErrorOccured?.Invoke(cbhOnUpdate.OccuredException);
                            }
                        };
                        return callbackId;
                    }
                    throw new Dacs7ReturnCodeException(returnCode);
                }
                throw new Dacs7ParameterException(errorCode);
            });
        }

        /// <summary>
        /// Remove the callback for alarms, so you will not get alarms any more.
        /// </summary>
        /// <param name="id">registration id created by register method</param>
        public void UnregisterAlarmUpdate(ushort id)
        {
            _alarmUpdateId = 0;
            ReleaseCallbackHandler(id);
        }

        /// <summary>
        /// Read the number of blocks in the PLC per type
        /// </summary>
        /// <returns>The current <see cref="DateTime"/> from the PLC.</returns>
        public DateTime GetPlcTime()
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateReadClockRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            Log($"GetPlcTime: ProtocolDataUnitReference is {id}");
            return (DateTime)PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                var errorCode = cbh.ResponseMessage.GetAttribute("ParamErrorCode", (ushort)0);
                if (errorCode == 0)
                {
                    var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
                    if (returnCode == 0xff)
                    {
                        var sslData = cbh.ResponseMessage.GetAttribute("SSLData", new byte[0]);
                        if (sslData.Any())
                            return sslData.ConvertToDateTime(2);
                        throw new InvalidDataException("SSL Data are empty!");
                    }
                    throw new Dacs7ReturnCodeException(returnCode);
                }
                throw new Dacs7ParameterException(errorCode);
            });
        }

        /// <summary>
        /// Read the number of blocks in the PLC per type
        /// </summary>
        /// <returns>The current <see cref="DateTime"/> from the PLC as a <see cref="Task"/></returns>
        public Task<DateTime> GetPlcTimeAsync()
        {
            return Task.Factory.StartNew(() => GetPlcTime(), _taskCreationOptions);
        }


        #region Helper

        private void OnClientStateChanged(string socketHandle, bool connected)
        {
            EventHandler?.Invoke(this, new PlcConnectionNotificationEventArgs(socketHandle, connected));
            Log(string.Format("OnClientStateChanged to {0}.", connected ? "connected" : "disconnected"));
            if (connected)
                Task.Factory.StartNew(() => ConfigurePlcConnection());
        }


        private void ConfigurePlcConnection()
        {
            try
            {
                _lastConnectException = null;
                _upperProtocolHandlerFactory.OnConnected();

                //Socket was Connected wait for Upper Protocol
                const int sliceSize = 10;
                var slice = (_timeout / sliceSize);
                for (var i = 0; i < slice; i += sliceSize)
                {
                    Thread.Sleep(sliceSize);
                    if (_clientSocket.IsConnected)
                        break;
                }

                //Upper Protocol was connected
                if (_clientSocket.IsConnected)
                {
                    var id = GetNextReferenceId();
                    var reqMsg = S7MessageCreator.CreateCommunicationSetup(id, _maxParallelJobs, PduSize);
                    var policy = new S7JobSetupProtocolPolicy();
                    Log(string.Format("Connect: ProtocolDataUnitReference is {0}", id));
                    PerformDataExchange(id, reqMsg, policy, (cbh) =>
                    {
                        var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                        if (errorClass == 0)
                        {
                            var data = cbh.ResponseMessage.GetAttribute("ParameterData", new byte[0]);
                            if (data.Length >= 7)
                            {
                                PduSize = data.GetSwap<UInt16>(5);
                                Log(string.Format("Connected: PduSize is {0}", PduSize));
                            }
                            return;
                        }
                        var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                        throw new Dacs7Exception(errorClass, errorCode);
                    });
                }
                else
                    throw new TimeoutException("Timeout while waiting for connection confirmation");
            }
            catch (Exception ex)
            {
                _lastConnectException = ex;
                Log(string.Format("ConfigurePlcConnection: Exception occurred {0}", ex.Message));
            }
            _waitingForPlcConfiguration.Set();
        }


        private void OnRawDataReceived(string socketHandle, IEnumerable<byte> buffer)
        {
            try
            {
                if (buffer != null)
                {
                    var b = buffer.ToArray();
                    foreach (var array in _upperProtocolHandlerFactory.RemoveUpperProtocolFrame(b, b.Length).Where(payload => payload != null))
                    {
                        Log($"OnRawDataReceived: Received Data size was {array.Length}");
                        var policy = GetProtocolPolicy(array);
                        if (policy != null)
                        {
                            Log($"OnRawDataReceived: determined policy is {policy.GetType().Name}");
                            var extractionResult = policy.ExtractRawMessages(array);
                            foreach (var msg in policy.Normalize(socketHandle, extractionResult.GetExtractedRawMessages()))
                            {
                                var id = msg.GetAttribute("ProtocolDataUnitReference", (ushort)0);
                                if (id == 0 && _alarmUpdateId != 0 && policy is S7UserDataAckAlarmUpdateProtocolPolicy)
                                    id = _alarmUpdateId;
                                Log(string.Format("OnRawDataReceived: ProtocolDataUnitReference is {0}", id));
                                _callbackLockSlim.EnterReadLock();
                                try
                                {
                                    if (_callbacks.TryGetValue(id, out CallbackHandler cb))
                                    {
                                        cb.ResponseMessage = msg;

                                        if (cb.Event != null)
                                            cb.Event.Set();
                                        else cb.OnCallbackAction?.Invoke(cb.ResponseMessage);
                                    }
                                    else
                                    {
                                        Log($"OnRawDataReceived: message with id {id} has no waiter!");
                                    }
                                }
                                finally
                                {
                                    _callbackLockSlim.ExitReadLock();
                                }
                            }
                        }
                    }
                }
                else
                    Log("OnRawDataReceived with empty data!");
            }
            catch (Exception ex)
            {
                Log($"OnRawDataReceived: Exception was {ex.Message} -{ex.StackTrace}");
                //Set the Exception to all pending Calls
                List<CallbackHandler> snapshot;
                _callbackLockSlim.EnterReadLock();
                try
                {
                    snapshot = _callbacks.Values.ToList();
                }
                finally
                {
                    _callbackLockSlim.ExitReadLock();
                }


                foreach (var cb in snapshot)
                {
                    cb.OccuredException = ex;
                    if (cb.Event != null)
                        cb.Event.Set();
                    else cb.OnCallbackAction?.Invoke(null);
                }

            }
        }

        private void AssignParameters()
        {
            _timeout = _parameter.GetParameter("Receive Timeout", 5000);
            _maxParallelJobs = _parameter.GetParameter("Maximum Parallel Jobs", (ushort)1);  //Used by simatic manager -> best performance with 1
            _maxParallelCalls = _parameter.GetParameter("Maximum Parallel Calls", (ushort)4); //Used by Dacs7
            _taskCreationOptions = _parameter.GetParameter("Use Threads", true) ? TaskCreationOptions.LongRunning : TaskCreationOptions.None; //Used by Dacs7
            _sleeptimeAfterMaxPendingCallsReached = _parameter.GetParameter("Sleeptime After Max Pending Calls Reached", 5);

            var config = new ClientSocketConfiguration
            {
                Hostname = _parameter.GetParameter("Ip", "127.0.0.1"),
                ServiceName = _parameter.GetParameter("Port", 102),
                ReceiveBufferSize = _parameter.GetParameter("ReceiveBufferSize", 65536),
                Autoconnect = _parameter.GetParameter("Reconnect", false),
                //_parameter.GetParameter("KeepAliveTime", default(uint)),
                //_parameter.GetParameter("KeepAliveInterval", default(uint))
            };

            //Setup the socket
            _clientSocket = new ClientSocket(config);

            var name = typeof(Rfc1006ProtocolHandler).Name;
            _upperProtocolHandlerFactory.RemoveProtocolHandler(name);
            _upperProtocolHandlerFactory.AddUpperProtocolHandler(new Rfc1006ProtocolHandler(
                _clientSocket.Send,
               "0x0100",
                CalcRemoteTsap(
                    _parameter.GetParameter("Connection Type", PlcConnectionType.Pg),
                    _parameter.GetParameter("Rack", ConnectionParameters.DefaultRack),
                    _parameter.GetParameter("Slot", ConnectionParameters.DefaultSlot)),
                 PduSize));
            _lastConnectException = null;
        }

        private UInt16 GetNextReferenceId()
        {
            var id = Interlocked.Increment(ref _referenceId);
            if (id < UInt16.MinValue || id > UInt16.MaxValue)
            {
                lock (_idLock)
                {
                    id = Interlocked.Increment(ref _referenceId);
                    if (id < UInt16.MinValue || id > UInt16.MaxValue)
                    {
                        Interlocked.Exchange(ref _referenceId, 0);
                        id = Interlocked.Increment(ref _referenceId);
                    }
                }
            }
            return Convert.ToUInt16(id);
        }

        private static string CalcRemoteTsap(PlcConnectionType connectionType, int rack, int slot)
        {
            var value = ((ushort)connectionType << 8) + (rack * 0x20) + slot;
            return string.Format("0x{0:X4}", value);
        }

        private void Log(string message)
        {
            OnLogEntry?.Invoke(message);
        }

        private object PerformDataExchange(ushort id, IMessage msg, IProtocolPolicy policy, Func<CallbackHandler, object> func)
        {
            try
            {
                var cbh = GetCallbackHandler(id);
                SendMessages(msg, policy);
                if (cbh.Event.WaitOne(_timeout))
                {
                    if (cbh.ResponseMessage != null)
                        return func(cbh);
                    if (cbh.OccuredException != null)
                        throw cbh.OccuredException;
                    else
                        throw new Exception("There was no response message created!");
                }
                else
                    throw new TimeoutException("Timeout while waiting for response.");
            }
            finally
            {
                ReleaseCallbackHandler(id);
            }
        }

        private void PerformDataExchange(ushort id, IMessage msg, IProtocolPolicy policy, Action<CallbackHandler> action)
        {
            try
            {
                var cbh = GetCallbackHandler(id);
                SendMessages(msg, policy);
                if (cbh.Event.WaitOne(_timeout))
                {
                    if (cbh.ResponseMessage != null)
                    {
                        action(cbh);
                        return;
                    }
                    if (cbh.OccuredException != null)
                        throw cbh.OccuredException;
                    throw new Exception("No response message was been created!");
                }
                else
                    throw new TimeoutException("Timeout while waiting for response.");
            }
            finally
            {
                ReleaseCallbackHandler(id);
            }
        }

        private CallbackHandler GetCallbackHandler(ushort id, bool withoutEvent = false)
        {
            //_semaphore.Wait();
            Interlocked.Increment(ref _currentNumberOfPendingCalls);
            AutoResetEvent arEvent = null;
            if (!withoutEvent)
            {
                _queueLockSlim.EnterUpgradeableReadLock();
                try
                {
                    if (_eventQueue.Any())
                    {
                        _queueLockSlim.EnterWriteLock();
                        try
                        {
                            arEvent = _eventQueue.Dequeue();
                        }
                        finally
                        {
                            _queueLockSlim.ExitWriteLock();
                        }
                    }
                    else
                        arEvent = new AutoResetEvent(false);
                }
                finally
                {
                    _queueLockSlim.ExitUpgradeableReadLock();
                }
            }

            var cbh = new CallbackHandler
            {
                Id = id,
                Event = arEvent,
            };

            _callbackLockSlim.EnterWriteLock();
            try
            {
                _callbacks.Add(id, cbh);
            }
            finally
            {
                _callbackLockSlim.ExitWriteLock();
            }


            return cbh;
        }

        private void ReleaseCallbackHandler(ushort id)
        {
            CallbackHandler cbh;

            _callbackLockSlim.EnterUpgradeableReadLock();
            try
            {
                if (_callbacks.TryGetValue(id, out cbh))
                {
                    _callbackLockSlim.EnterWriteLock();
                    try
                    {
                        _callbacks.Remove(id);
                    }
                    finally
                    {
                        _callbackLockSlim.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _callbackLockSlim.ExitUpgradeableReadLock();
            }


            if (cbh != null && cbh.Event != null)
            {
                _queueLockSlim.EnterWriteLock();
                try
                {
                    _eventQueue.Enqueue(cbh.Event);
                    Log($"Number of queued events {_eventQueue.Count}");
                }
                finally
                {
                    _queueLockSlim.ExitWriteLock();
                }
            }

            //_semaphore.Release();
            Interlocked.Decrement(ref _currentNumberOfPendingCalls);
        }

        private async void SendMessages(IMessage msg, IProtocolPolicy policy)
        {
            foreach (var data in policy.TranslateToRawMessage(msg))
            {
                await Send(data);
            }
        }

        private async Task Send(IEnumerable<byte> msg)
        {
            var ret = await _clientSocket.Send(_upperProtocolHandlerFactory.AddUpperProtocolFrame(msg.ToArray()));
            if (ret != SocketError.Success)
                throw new SocketException((int)ret);
        }

        private static IProtocolPolicy GetProtocolPolicy(IEnumerable<byte> data)
        {
            var policy = ProtocolPolicyBase.FindPolicyByPayload(data);
            return policy ?? new S7AckDataProtocolPolicy();
        }

        private static byte[] ExtractAssotiatedValue(IMessage msg, int alarmindex)
        {
            var subItemName = $"Alarm[{alarmindex}].ExtendedData[0]." + "{0}";
            if (msg.GetAttribute(string.Format(subItemName, "NumberOfAssotiatedValues"), 0) > 0)
            {
                return msg.GetAttribute(string.Format(subItemName, "AssotiatedValue"), new byte[0]);
            }
            return new byte[0];
        }

        private static DateTime ExtractTimestamp(IMessage msg, int alarmindex, int tsIdx = 0)
        {
            var subItemName = $"Alarm[{alarmindex}].ExtendedData[{tsIdx}]." + "{0}";
            return msg.GetAttribute(string.Format(subItemName, "Timestamp"), DateTime.MinValue);
        }


        /// <summary>
        /// Splits the operations into parts if necessary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters">All parameters requested</param>
        /// <returns></returns>
        private List<IEnumerable<T>> GetOperationParts<T>(IEnumerable<T> parameters) where T : OperationParameter
        {
            const int headerSize = 12; // 10 Job header + 2 Parameter //  12 item header size
            var currentSize = headerSize;
            var currentPackage = new List<T>();
            var result = new List<IEnumerable<T>> { currentPackage };
            foreach (var parameter in parameters)
            {
                var itemSize = headerSize;

                if (parameter is WriteOperationParameter)
                {
                    itemSize += 4 + parameter.Length;  // Data header = 4
                    if (parameter.Type == typeof(string))
                        itemSize += 2;

                    if (itemSize % 2 != 0) itemSize++;
                }

                currentSize = currentSize + itemSize;
                if (PduSize <= currentSize)
                {
                    currentSize = headerSize + itemSize; // reset size
                    currentPackage = new List<T>(); // create new package
                    result.Add(currentPackage); // add it to result
                }
                currentPackage.Add(parameter);
            }
            return result;
        }

        private static object ExtractData(object data, int offset = 0, int length = Int32.MaxValue)
        {
            var enumerable = data as byte[];
            if (enumerable == null)
            {
                var boolEnum = data as bool[];
                if (boolEnum == null)
                {
                    if (data is bool)
                        return (bool)data;
                    if (data is byte || data is char)
                        return (byte)data;
                    return null;
                }
                return boolEnum.SubArray(offset, length);
            }
            return enumerable.SubArray(offset, length);
        }

        private static string ResolveErrorCode<T>(byte b) where T : struct
        {
            return Enum.IsDefined(typeof(T), b) ? ResolveErrorCode<T>(Enum.GetName(typeof(T), b)) : b.ToString(CultureInfo.InvariantCulture);
        }

        private static string ResolveErrorCode<T>(ushort sh) where T : struct
        {
            return Enum.IsDefined(typeof(T), sh) ? ResolveErrorCode<T>(Enum.GetName(typeof(T), sh)) : sh.ToString(CultureInfo.InvariantCulture);
        }

        private static string ResolveErrorCode<T>(string s) where T : struct
        {
            if (Enum.TryParse(s, out T result))
            {
                var r = GetEnumDescription(result);
                if (!r.IsNullOrEmpty())
                    return r;
            }
            return s;
        }

        private static string GetEnumDescription(object e)
        {
            var fieldInfo = e.GetType().GetField(e.ToString());
            if (fieldInfo != null)
            {
                if (fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] enumAttributes && enumAttributes.Length > 0)
                    return enumAttributes[0].Description;
            }
            return e.ToString();
        }
        #endregion
    }
}
