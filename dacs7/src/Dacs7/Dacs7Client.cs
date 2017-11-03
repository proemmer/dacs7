using Dacs7.Communication;
using Dacs7.Domain;
using Dacs7.Helper;
using Dacs7.Protocols;
using Dacs7.Protocols.RFC1006;
using Dacs7.Protocols.S7;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public partial class Dacs7Client : IDacs7Client
    {
        #region Fields
        private readonly ILogger _logger;
        private readonly object _syncRoot = new object();
        private Exception _lastConnectException = null;
        private readonly AutoResetEvent _waitingForPlcConfiguration = new AutoResetEvent(false);
        private readonly UpperProtocolHandlerFactory _upperProtocolHandlerFactory = new UpperProtocolHandlerFactory();
        private readonly Queue<AutoResetEvent> _eventQueue = new Queue<AutoResetEvent>();
        private ReaderWriterLockSlim _queueLockSlim = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim _callbackLockSlim = new ReaderWriterLockSlim();     
        private int _currentNumberOfPendingCalls;
        private int _sleeptimeAfterMaxPendingCallsReached;
        private ushort _maxParallelJobs;
        private ushort _maxParallelCalls;
        private bool _disableParallelSemaphore;
        private TaskCreationOptions _taskCreationOptions = TaskCreationOptions.None;
        private ConnectionParameters _parameter;
        private ClientSocket _clientSocket;
        private readonly Dictionary<int, CallbackHandler> _callbacks = new Dictionary<int, CallbackHandler>();
        private string _connectionString = string.Empty;
        private int _timeout = 5000;
        private int _connectTimeout = 5000;
        private const ushort PduSizeDefault = 960;
        private const ushort MaxParallelJobsDefault = 2;
        private int _referenceId;
        private readonly object _idLock = new object();
        private ushort _receivedPduSize;
        private ushort _receivedMaxParallelJobs;
        private readonly object _eventHandlerLock = new object();
        private event OnConnectionChangeEventHandler EventHandler;
        private SemaphoreSlim _parallelCallsSemaphore;

        private Dictionary<Type,ushort> _callbackIds = new Dictionary<Type, ushort>();
        #endregion

        #region Properties

        internal TaskCreationOptions TaskCreationOptions => _taskCreationOptions;
        internal ILogger Logger => _logger;

        internal bool TrySetCallbackId(Type policyType, ushort id = 0)
        {
            var contains = _callbackIds.ContainsKey(policyType);
            if (id > 0 && !contains)
            {
                lock(_callbackIds)
                {
                    if (!_callbackIds.ContainsKey(policyType))
                    {
                        _callbackIds.Add(policyType, id);
                    }
                }
            }
            else if(id == 0 && contains)
            {
                lock (_callbackIds)
                {
                    return _callbackIds.Remove(policyType);
                }
            }
            return false;
        }

        internal bool HasCallbackId(Type policyType)
        {
            return GetCallbackId(policyType) != 0;
        }

        internal ushort GetCallbackId(Type policyType)
        {
            if(_callbackIds.TryGetValue(policyType, out ushort id))
            {
                return id;
            }
            return 0;
        }

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

        public UInt16 MaximumParallelJobs
        {
            get { return _receivedMaxParallelJobs <= 0 ? _parameter.GetParameter("Maximum Parallel Jobs", MaxParallelJobsDefault) : _receivedMaxParallelJobs; }
            private set
            {
                if (value < _parameter.GetParameter("Maximum Parallel Jobs", MaxParallelJobsDefault)) 
                    _receivedMaxParallelJobs = value;
            }
        }


        private UInt16 ItemReadSlice { get { return (UInt16)(PduSize - 18); } }  //18 Header and some other data 
        private UInt16 ItemWriteSlice { get { return (UInt16)(PduSize - 28); } } //28 Header and some other data

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
        /// Client constructor to register acknowledge policies.
        /// </summary>
        public Dacs7Client(ILoggerFactory factory) : this(factory.CreateLogger<Dacs7Client>())
        {

        }

        /// <summary>
        /// Client constructor to register acknowledge policies.
        /// </summary>
        public Dacs7Client(ILogger logger = null)
        {
            _logger = logger;
            // Register needed ack policies
            new S7AckDataProtocolPolicy();
            new S7ReadJobAckDataProtocolPolicy();
            new S7WriteJobAckDataProtocolPolicy();
            new S7UserDataAckAlarmUpdateProtocolPolicy();
            new S7UserDataAckPendingRequestProtocolPolicy();
        }

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
                    _callbackLockSlim = null;
                }
                catch (ObjectDisposedException)
                {
                }
            }

            if (_parallelCallsSemaphore != null)
            {
                try
                {
                    _parallelCallsSemaphore.Dispose();
                    _parallelCallsSemaphore = null;
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

                if (IsConnected)
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
            return Task.Factory.StartNew(() => Connect(connectionString));
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
                        _logger?.LogError($"Exception on Disconnect. Error was: {ex.Message}");
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
                    foreach(var item in _callbackIds.Where(item => item.Value > 0))
                        UnregisterCallbackId(item.Key, item.Value);
                    _clientSocket.Close();
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Exception on Disconnect while closing socket. Error was: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Disconnect from the plc asynchronous
        /// </summary>
        public Task DisconnectAsync()
        {
            return Task.Factory.StartNew(Disconnect);
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

            var oringinOffset = offset;
            SetupGenericReadData<T>(ref offset, out Type readType, out int bytesToRead);
            var data = ReadAny(PlcArea.DB, oringinOffset, readType, new[] { bytesToRead, dbNumber });

            return data != null && data.Any() ? (T)data.ConvertTo<T>() : default(T);
        }

        /// <summary>
        /// Read data async from the given data block number at the given offset.
        /// The length of the data will be extracted from the generic parameter.
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <returns>The read value or the default value of the type if the data could not be read.</returns>
        public async Task<T> ReadAnyAsync<T>(int dbNumber, int offset)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();

            var oringinOffset = offset;
            SetupGenericReadData<T>(ref offset,out Type readType, out int bytesToRead);
            var data = await ReadAnyAsync(PlcArea.DB, oringinOffset, readType, new[] { bytesToRead, dbNumber });

            return data != null && data.Any() ? (T)data.ConvertTo<T>() : default(T);
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

            SetupGenericReadData<T>(ref offset, out Type readType, out int bytesToRead, out int elementLength, out int bitOffset, out Type t, out bool isBool, out bool isString, numberOfItems);
            var data = ReadAny(PlcArea.DB, offset, readType, new[] { bytesToRead, dbNumber });

            return ConvertToEnumerable<T>(numberOfItems, t, isBool, isString, elementLength, bitOffset, bytesToRead, data);
        }

        /// <summary>
        /// Read a number of items async from the given generic type form the given data block number at the given offset and try convert it to the given dataType.
        /// </summary>
        /// <typeparam name="TElement">Element type of the resulting enumerable</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="numberOfItems">Number of items of the T to read. This could be the string length for a string, or the number of bytes/int for an array and so on. 
        /// The default value is always 1.</param>
        /// <returns>A list of TElement</returns>
        public async Task<IEnumerable<T>> ReadAnyAsync<T>(int dbNumber, int offset, int numberOfItems)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();

            SetupGenericReadData<T>(ref offset, out Type readType, out int bytesToRead, out int elementLength, out int bitOffset, out Type t, out bool isBool, out bool isString, numberOfItems);
            var data = await ReadAnyAsync(PlcArea.DB, offset, readType, new[] { bytesToRead, dbNumber });

            return ConvertToEnumerable<T>(numberOfItems, t, isBool, isString, elementLength, bitOffset, bytesToRead, data);
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
            SetupParameter(args, out ushort length, out ushort dbNr, out S7JobReadProtocolPolicy policy);
            return GetReadReferences(offset, length).SelectMany(item => ProcessReadOperation(area, offset, type, dbNr, policy, item)).ToArray();
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

                _logger?.LogDebug($"ReadAny: ProtocolDataUnitReference is {id}");
                if (PerformDataExchange(id, reqMsg, policy, (cbh) =>
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
                var numberOfItems = param.Args != null && param.Args.Length > 0 ? param.Args[0] : 1;
                var offset = param.Offset;
                SetupGenericReadData(param.Type, ref offset, out Type readType, out int bytesToRead, out int elementLength, out int bitOffset, out Type t, out bool isBool, out bool isString, numberOfItems);
                
                resultList.Add(ConvertToType(numberOfItems, t, isBool, isString, elementLength, bitOffset, bytesToRead, data));
            }
            return resultList;
        }

        /// <summary>
        /// Read multiple variables with one call from the PLC and return them as a list of the correct read types.
        /// </summary>
        /// <param name="parameters">A list of <see cref="ReadOperationParameter"/>, so multiple read requests can be handled in one message</param>
        /// <returns>A list of <see cref="T:object"/> where every list entry contains the read value in order of the given parameter order</returns>
        public Task<IEnumerable<object>> ReadAnyAsync(IEnumerable<ReadOperationParameter> parameters)
        {
            //TODO:  _maxParallelCalls <= 1 ?
            return Task.Factory.StartNew(() => ReadAny(parameters), _taskCreationOptions);
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
                SetupParameter(args, out ushort length, out ushort dbNr, out S7JobReadProtocolPolicy policy);
                var items = GetReadReferences(offset, length).ToList();
                Parallel.ForEach(items, item => ProcessReadOperation(area, offset, type, dbNr, policy, item));

                return items.OrderBy(x => x.DataOffset)
                            .SelectMany(request => request.Data as byte[] ?? throw new InvalidDataException("Returned data are null")).ToArray();

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
            return  _maxParallelCalls <= 1 ?
                Task.Factory.StartNew(() => ReadAny(area, offset, type, args), _taskCreationOptions) :
                ReadAnyPartsAsync(area, offset, type, args);
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
            if (elementType == typeof(bool))
            {
                //with bool's we have to create a multi write request
                WriteAny((value as IEnumerable<bool>).Select((element, i) => WriteOperationParameter.Create(area, offset + i, element)));
            }
            else
            {
                WriteAny(area, offset, value, new int[] { size });
            }
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
            }
            else
            {
                WriteAny(PlcArea.DB, offset, value, new int[] { size, dbNumber });
            }
            
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

            SetupParameter(args, out ushort length, out ushort dbNr, out S7JobWriteProtocolPolicy policy);
            GetWriteReferences(offset, length, value).ForEach(item => ProcessWriteOperation(area, dbNr, policy, item));
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
                SetupParameter(args, out ushort length, out ushort dbNr, out S7JobWriteProtocolPolicy policy);
                Parallel.ForEach(GetWriteReferences(offset, length, value), item => ProcessWriteOperation(area, dbNr, policy, item));
            }
            catch (AggregateException exception)
            {
                //Throw only the first exception
                throw exception.InnerExceptions.First();
            }
        }

        /// <summary>
        /// Write data async to the given PLC data block with offset and length.
        /// </summary>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="length">the length of data in bytes you want to write  e.g. if you have a value byte[500] and you want to write
        /// only the first 100 bytes, you have to set length to 100. If length is not set, the correct size will be determined by the value size.</param>
        public async Task WriteAnyAsync<T>(int dbNumber, int offset, T value, int length = -1)
        {
            var size = CalculateSizeForGenericWriteOperation(PlcArea.DB, value, length, out Type elementType);
            if (elementType == typeof(bool))
            {
                //with bool's we have to create a multi write request
                await WriteAnyAsync((value as IEnumerable<bool>).Select((element, i) => WriteOperationParameter.Create(dbNumber, offset + i, element)));
            }
            await WriteAnyAsync(PlcArea.DB, offset, value, new[] { size, dbNumber });
        }

        /// <summary>
        /// Write data async to the given PLC area.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="length">the length of data in bytes you want to write  e.g. if you have a value byte[500] and you want to write
        /// only the first 100 bytes, you have to set length to 100. If length is not set, the correct size will be determined by the value size.</param>
        public Task WriteAnyAsync<T>(PlcArea area, int offset, T value, int length = -1)
        {
            if (area == PlcArea.DB)
                throw new ArgumentException("The argument area could not be DB.");
            var size = CalculateSizeForGenericWriteOperation(PlcArea.DB, value, length, out Type elementType);
            if (elementType == typeof(bool))
            {
                //with bool's we have to create a multi write request
                return WriteAnyAsync((value as IEnumerable<bool>).Select((element, i) => WriteOperationParameter.Create(area, offset + i, element)));
            }
            return WriteAnyAsync(area, offset, value, new[] { size });
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
        public async Task WriteAnyAsync(PlcArea area, int offset, object value, params int[] args)
        {
            if (_maxParallelCalls <= 1)
            {
                await Task.Factory.StartNew(() =>
                {
                    WriteAny(area, offset, value, args);
                }, _taskCreationOptions);
            }
            else
                await WriteAnyPartsAsync(area, offset, value, args);
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

                _logger?.LogDebug($"WriteAny: ProtocolDataUnitReference is {id}");
                PerformDataExchange(id, reqMsg, policy, (cbh) =>
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
                });
            }
        }

        /// <summary>
        /// Write multiple variables async with one call to the PLC.
        /// </summary>
        /// <param name="parameters">A list of <see cref="WriteOperationParameter"/>, so multiple write requests can be handled in one message</param>
        public Task WriteAnyAsync(IEnumerable<WriteOperationParameter> parameters)
        {
            return Task.Factory.StartNew(() => WriteAny(parameters), _taskCreationOptions);
        }





        #region connection helper

        private void OnClientStateChanged(string socketHandle, bool connected)
        {
            EventHandler?.Invoke(this, new PlcConnectionNotificationEventArgs(socketHandle, connected));
            _logger?.LogInformation($"OnClientStateChanged to {(connected ? "connected" : "disconnected")}.");
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
                    var reqMsg = S7MessageCreator.CreateCommunicationSetup(id, MaximumParallelJobs, PduSize);
                    var policy = new S7JobSetupProtocolPolicy();
                    _logger?.LogDebug($"Connect: ProtocolDataUnitReference is {id}");
                    PerformDataExchange(id, reqMsg, policy, (cbh) =>
                    {
                        var data = cbh.ResponseMessage.GetAttribute("ParameterData", new byte[0]);
                        if (data.Length >= 7)
                        {
                            PduSize = data.GetSwap<UInt16>(5);
                            _logger?.LogInformation($"Connected: PduSize is {PduSize}");

                            var jobs = data.GetSwap<UInt16>(1);
                            if(_parallelCallsSemaphore == null || MaximumParallelJobs != jobs)
                            {
                                MaximumParallelJobs = jobs;
                                if (!_disableParallelSemaphore)
                                {
                                    _parallelCallsSemaphore = new SemaphoreSlim(jobs);
                                }
                                else
                                {
                                    _parallelCallsSemaphore?.Dispose();
                                    _parallelCallsSemaphore = null;
                                }
                            }
                            _logger?.LogInformation($"Connected: MaximumParallelJobs is {MaximumParallelJobs}");
                            
                        }
                        return;
                    });
                }
                else
                    throw new TimeoutException("Timeout while waiting for connection confirmation");
            }
            catch (Exception ex)
            {
                _lastConnectException = ex;
                _logger?.LogError($"ConfigurePlcConnection: Exception occurred {ex.Message}");
            }
            finally
            {
                _waitingForPlcConfiguration.Set();
            }
        }

        #endregion

        #region config

        private void AssignParameters()
        {
            _timeout = _parameter.GetParameter("Receive Timeout", 5000);
            _maxParallelJobs = _parameter.GetParameter("Maximum Parallel Jobs", MaximumParallelJobs); 

            // invalid value means disable semaphore
            if(_maxParallelJobs == 0)
            {
                _maxParallelJobs = 2;  // simatic manager default value
                _disableParallelSemaphore = true;
            }

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
        #endregion

        #region Communication


        private ushort HandleCallbackIds(ushort id, IProtocolPolicy policy)
        {
            if (id == 0)
            {
                foreach (var item in _callbackIds.Where(x => x.Value > 0))
                {
                    if(policy.GetType() == item.Key)
                    {
                        return item.Value;
                    }
                }
            }
            return id;
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
                        _logger?.LogDebug($"OnRawDataReceived: Received Data size was {array.Length}");
                        var policy = ProtocolPolicyBase.FindPolicyByPayload<S7AckDataProtocolPolicy>(array);
                        if (policy != null)
                        {
                            _logger?.LogDebug($"OnRawDataReceived: determined policy is {policy.GetType().Name}");
                            var extractionResult = policy.ExtractRawMessages(array);
                            foreach (var msg in policy.Normalize(socketHandle, extractionResult.GetExtractedRawMessages()))
                            {
                                var id = HandleCallbackIds(msg.GetAttribute("ProtocolDataUnitReference", (ushort)0), policy);

                                _logger?.LogDebug($"OnRawDataReceived: ProtocolDataUnitReference is {id}");
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
                                        _logger?.LogWarning($"OnRawDataReceived: message with id {id} has no waiter!");
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
                    _logger?.LogDebug("OnRawDataReceived with empty data!");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"OnRawDataReceived: Exception was {ex.Message} - {ex.StackTrace}");
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

        internal UInt16 GetNextReferenceId()
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

        private T PerformInSemaphore<T>(ushort id, Func<T> action)
        {
            try
            {
                if (_parallelCallsSemaphore != null && !_parallelCallsSemaphore.Wait(_timeout))
                {
                    throw new TimeoutException($"Timeout while waiting for free send slot for ProtocolDataUnitReference {id}.");
                }

                return action();
            }
            finally
            {
                _parallelCallsSemaphore?.Release();
            }
        }

        internal object PerformDataExchange(ushort id, IMessage msg, IProtocolPolicy policy, Func<CallbackHandler, object> func)
        {
            return PerformInSemaphore(id, () =>
            {
                try
                {
                    var cbh = GetCallbackHandler(id);
                    var sending = SendMessages(msg, policy);
                    if (cbh.Event.WaitOne(_timeout))
                    {
                        if (cbh.ResponseMessage != null)
                            return func(cbh);
                        if (cbh.OccuredException != null)
                            throw cbh.OccuredException;
                        else
                            throw new Exception($"There was no response message created for ProtocolDataUnitReference {id}!");
                    }
                    else
                    {
                        if (sending.Exception != null)
                            throw sending.Exception;
                        else
                            throw new TimeoutException($"Timeout while waiting for response for ProtocolDataUnitReference {id}.");
                    }
                }
                finally
                {
                    ReleaseCallbackHandler(id);
                }
            });
        }

        private void PerformDataExchange(ushort id, IMessage msg, IProtocolPolicy policy, Action<CallbackHandler> action)
        {
            PerformInSemaphore(id, () =>
            {
                try
                {
                    var cbh = GetCallbackHandler(id);
                    var sending = SendMessages(msg, policy);
                    if (cbh.Event.WaitOne(_timeout))
                    {
                        if (cbh.ResponseMessage != null)
                        {
                            cbh.ResponseMessage.EnsureValidErrorClass(0x00);
                            action(cbh);
                            return 0;
                        }
                        if (cbh.OccuredException != null)
                            throw cbh.OccuredException;
                        throw new Exception($"There was no response message created for ProtocolDataUnitReference {id}!");
                    }
                    else
                    {
                        if (sending.Exception != null)
                            throw sending.Exception;
                        else
                            throw new TimeoutException($"Timeout while waiting for response for ProtocolDataUnitReference {id}.");
                    }
                }
                finally
                {
                    ReleaseCallbackHandler(id);
                }
            });
        }

        internal CallbackHandler GetCallbackHandler(ushort id, bool withoutEvent = false)
        {
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

        internal void ReleaseCallbackHandler(ushort id)
        {
            CallbackHandler cbh;
            try
            {

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
                        _logger?.LogDebug($"Number of queued events {_eventQueue.Count}");
                    }
                    finally
                    {
                        _queueLockSlim.ExitWriteLock();
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _currentNumberOfPendingCalls);
            }

        }

        /// <summary>
        /// Remove the callback for the given id.
        /// </summary>
        /// <param name="name">name of the callback</param>
        /// <param name="id">registration id created by register method</param>
        internal void UnregisterCallbackId(Type policyType, ushort id)
        {
            TrySetCallbackId(policyType, 0);
            ReleaseCallbackHandler(id);
        }

        private async Task SendMessages(IMessage msg, IProtocolPolicy policy)
        {
            try
            {
                foreach (var data in policy.TranslateToRawMessage(msg))
                {
                    await Send(data);
                }
            }
            catch(Exception)
            {
                throw;
            }
        }

        private async Task Send(IEnumerable<byte> msg)
        {
            try
            {
                var sendData = _upperProtocolHandlerFactory.AddUpperProtocolFrame(msg.ToArray());
                if (!_clientSocket.IsConnected)
                {
                    var errorMsg = $"Could not send data, because socket is currently disconnected!";
                    _logger?.LogError(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }
                var ret = await _clientSocket.Send(sendData);
                if (ret != SocketError.Success)
                    throw new SocketException((int)ret);
            }
            catch(Exception ex)
            {
                _logger?.LogError($"Socket send exception: {ex.Message}");
                throw;
            }
        }


        #endregion

        #region Helper




        /// <summary>
        /// Read data from the plc by using Tasks.
        /// </summary>
        /// <param name="area">Specify the plc area to read.  e.g. IB InputByte</param>
        /// <param name="offset">Specify the read offset</param>
        /// <param name="type">Specify the .Net data type for the red data</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        private async Task<byte[]> ReadAnyPartsAsync(PlcArea area, int offset, Type type, params int[] args)
        {
            if (!IsConnected)
                throw new Dacs7NotConnectedException();
            try
            {
                SetupParameter(args, out ushort length, out ushort dbNr, out S7JobReadProtocolPolicy policy);

                var requests = new List<Task<byte[]>>();
                GetReadReferences(offset, length).ForEach(item =>
                {
                    requests.Add(Task.Factory.StartNew(() =>
                    {
                        while (_currentNumberOfPendingCalls >= _maxParallelCalls)
                            Thread.Sleep(_sleeptimeAfterMaxPendingCallsReached);
                        return ProcessReadOperation(area, offset, type, dbNr, policy, item);
                    }, _taskCreationOptions));
                });

                await Task.WhenAll(requests.ToArray());

                return requests.SelectMany(request => request.Result as byte[] ?? throw new InvalidDataException("Returned data are null")).ToArray();
            }
            catch (AggregateException exception)
            {
                //Throw only the first exception
                throw exception.InnerExceptions.First();
            }
        }

        /// <summary>
        /// Write data parallel to the connected plc.
        /// </summary>
        /// <param name="area">Specify the plc area to write to.  e.g. OB OutputByte</param>
        /// <param name="offset">Specify the write offset</param>
        /// <param name="value">Value to write</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        private async Task WriteAnyPartsAsync(PlcArea area, int offset, object value, params int[] args)
        {
            try
            {
                if (!IsConnected)
                    throw new Dacs7NotConnectedException();

                var requests = new List<Task>();
                SetupParameter(args, out ushort length, out ushort dbNr, out S7JobWriteProtocolPolicy policy);
                foreach (var item in GetWriteReferences(offset, length, value))
                {
                    requests.Add(Task.Factory.StartNew(() =>
                    {
                        while (_currentNumberOfPendingCalls >= _maxParallelCalls)
                            Thread.Sleep(_sleeptimeAfterMaxPendingCallsReached);
                        ProcessWriteOperation(area, dbNr, policy, item);
                    }));
                }

                await Task.WhenAll(requests);
            }
            catch (AggregateException exception)
            {
                //Throw only the first exception
                throw exception.InnerExceptions.First();
            }
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
                var itemSize = UpdateWriteParameterSize(parameter, headerSize);
                currentSize += itemSize;

                if (currentSize >= PduSize)
                {
                    do
                    {
                        var origin = parameter;
                        var tmpParam = origin;
                        do
                        {
                            tmpParam = currentSize > PduSize ? (T)origin.Cut(GetItemLength(origin)) : origin;
                            itemSize = UpdateWriteParameterSize(tmpParam, headerSize);

                            if (currentPackage.Any())
                            {
                                currentSize = headerSize + itemSize; // reset size   
                                currentPackage = new List<T>(); // create new package
                                result.Add(currentPackage); // add it to result
                            }
                            currentPackage.Add(tmpParam);
                        }
                        while (origin.Length > GetItemLength(origin));
                    }
                    while (currentSize >= PduSize);
                }
                else if(parameter.Length > 0)
                    currentPackage.Add(parameter);


            }
            return result;
        }

        private int GetItemLength<T>(T parameter) where T : OperationParameter
        {
            if (parameter is WriteOperationParameter)
            {
                return ItemWriteSlice;
            }
            return ItemReadSlice;
        }

        private static int UpdateWriteParameterSize<T>(T parameter, int itemSize) where T : OperationParameter
        {
            if (parameter is WriteOperationParameter)
            {
                itemSize += 4 + parameter.Length;  // Data header = 4
                if (parameter.Type == typeof(string))
                    itemSize += 2;

                if (itemSize % 2 != 0) itemSize++;
            }
            return itemSize;
        }

        public static object InvokeGenericMethod<T>(Type genericType, string methodName, object[] parameters)
        {
            var method = parameters.All(x => x != null)
                                ? typeof(T).GetMethod(methodName, parameters.Select(x => x.GetType()).ToArray())
                                : typeof(T).GetMethod(methodName);
            var genericMethod = method.MakeGenericMethod(genericType);
            return genericMethod.Invoke(null, parameters);
        }

        private static void SetupGenericReadData<T>(ref int offset, out Type readType, out int bytesToRead)
        {
            SetupGenericReadData<T>(ref offset, out readType, out bytesToRead, out _ ,out _, out _, out _, out _);
        }

        private static object SetupGenericReadData(Type genericType, ref int offset, out Type readType, out int bytesToRead, out int elementLength, out int bitOffset, out Type t, out bool isBool, out bool isString, int numberOfItems = 1)
        {
            readType = t = null; 
            bytesToRead = elementLength = bitOffset = 0;
            isBool = isString = false;

            var parameters = new object[] { offset, readType, bytesToRead, elementLength, bitOffset, t, isBool, isString, numberOfItems };
            var result = InvokeGenericMethod<Dacs7Client>(genericType, nameof(SetupGenericReadData), parameters);
            offset = (int)parameters[0];
            readType = (Type)parameters[1];
            bytesToRead = (int)parameters[2];
            elementLength = (int)parameters[3];
            bitOffset = (int)parameters[4];
            t = (Type)parameters[5];
            isBool = (bool)parameters[6];
            isString = (bool)parameters[7];
            return result;
        }

        public static void SetupGenericReadData<T>(ref int offset, out Type readType, out int bytesToRead, out int elementLength, out int bitOffset, out Type t, out bool isBool, out bool isString, int numberOfItems = 1)
        {
            t = typeof(T);
            isBool = t == typeof(bool);
            isString = t == typeof(string);
            readType = (isBool && numberOfItems <= 1) ? typeof(bool) : typeof(byte);
            
            bytesToRead = 1;
            bitOffset = 0;
            var originOffset = offset;
            if (numberOfItems > 0)
            {
                if (isBool)
                {
                    offset /= 8;
                    bitOffset = originOffset % 8;
                    var bitsToRead = bitOffset + numberOfItems;
                    elementLength = bitsToRead / 8 + (bitsToRead % 8 > 0 ? 1 : 0);
                    bytesToRead = elementLength;
                }
                else if (isString)
                {
                    bytesToRead = elementLength = numberOfItems + 2;
                }
                else
                {
                    elementLength = Marshal.SizeOf<T>();
                    bytesToRead = numberOfItems * elementLength;
                }
            }
            else
                bytesToRead = elementLength = 0;
        }

        private object ConvertToType(int numberOfItems, Type t, bool isBool, bool isString, int elementLength, int bitOffset, int bytesToRead, byte[] data)
        {
            var m = typeof(Dacs7Client).GetMethods().ToList();
            var method = this.GetType().GetMethod("ConvertTo");
            var genericMethod = method.MakeGenericMethod(t);
            return genericMethod.Invoke(null, new object[] { numberOfItems, t, isBool, isString, elementLength, bitOffset, bytesToRead, data });
        }


        public static object ConvertTo<T>(int numberOfItems, Type t, bool isBool, bool isString, int elementLength, int bitOffset, int bytesToRead, byte[] data)
        {
            if (isString)
            {
                var result = new List<T>();
                string s = string.Empty;
                if (data.Length > 2)
                {
                    var length = (int)data[1];
                    if (length > data.Length - 2)
                        s = string.Empty; // INVALID DATA
                    else
                        s = new String(data.Skip(2).Select(x => Convert.ToChar(x)).ToArray()).Substring(0, length);
                }
                result.Add((T)Convert.ChangeType(s, t));
                return result;
            }
            else if (t != typeof(byte) && t != typeof(char) && numberOfItems > 1)
            {
                var result = new List<T>();
                var array = data as byte[];
                var lengthInBits = array.Length * 8;
                for (int i = 0; i < numberOfItems; i += elementLength)
                {
                    if (isBool)
                    {
                        var bitIdx = bitOffset + i;
                        if (bitIdx >= lengthInBits)
                        {
                            throw new IndexOutOfRangeException($"Bit-Index {bitIdx} is not in the array!");
                        }

                        result.Add((T)Convert.ChangeType(array.GetBit(bitIdx), t));
                    }
                    else
                    {
                        if (i >= array.Length)
                        {
                            throw new IndexOutOfRangeException($"Index {i} is not in the array!");
                        }

                        result.Add((T)data.ConvertTo<T>(i));
                    }
                }
                return result;
            }
            else if ((t == typeof(byte) || t == typeof(char)) && numberOfItems == 1)
            {
                return (T)Convert.ChangeType(data[0], t);
            }
            return data.ConvertTo<T>();
        }

        private IEnumerable<T> ConvertToEnumerable<T>(int numberOfItems, Type t, bool isBool, bool isString, int elementLength, int bitOffset, int bytesToRead, byte[] data)
        {
            var result = ConvertTo<T>(numberOfItems, t, isBool, isString, elementLength, bitOffset, bytesToRead, data);
            if(result is IEnumerable<T>)
            {
                return (IEnumerable<T>)result;
            }
            var resultList = new List<T>
            {
                (T)result
            };
            return resultList;
        }

        private void SetupParameter<T>(int[] args, out ushort length, out ushort dbNr, out T policy) where T : S7ProtocolPolicy, new()
        {
            length = Convert.ToUInt16(args.Any() ? args[0] : 0);
            dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
            policy = new T();
        }

        private IEnumerable<WriteReference> GetWriteReferences(int offset, ushort length, object data)
        {
            var requests = new List<WriteReference>();
            if (length > ItemWriteSlice)
            {
                var packageLength = length;
                for (var j = 0; j < length; j += ItemWriteSlice)
                {
                    var writeLength = Math.Min(ItemWriteSlice, packageLength);
                    packageLength -= ItemWriteSlice;
                    yield return new WriteReference(j, offset + j, writeLength, data);
                }
            }
            else
                yield return new WriteReference(0, offset, length, data, false);
        }

        private IEnumerable<ReadReference> GetReadReferences(int offset, ushort length)
        {
            var requests = new List<ReadReference>();
            var packageLength = length;
            var readResult = new List<byte>();
            for (var j = 0; j < length; j += ItemReadSlice)
            {
                var readLength = Math.Min(ItemReadSlice, packageLength);
                packageLength -= ItemReadSlice;
                yield return new ReadReference(j, offset + j, readLength);
            }
        }

        private void ProcessWriteOperation(PlcArea area, ushort dbNr, S7JobWriteProtocolPolicy policy, WriteReference item)
        {
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateWriteRequest(id, area, dbNr, item.PlcOffset, item.Length, item.Data);
            _logger?.LogDebug($"WriteAny: ProtocolDataUnitReference is {id}");

            PerformDataExchange(id, reqMsg, policy, (cbh) =>
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
            });
        }

        private byte[] ProcessReadOperation(PlcArea area, int offset, Type type, ushort dbNr, S7JobReadProtocolPolicy policy, ReadReference item)
        {
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateReadRequest(id, area, dbNr, item.PlcOffset, item.Length, type);
            _logger?.LogDebug($"ReadAny: ProtocolDataUnitReference is {id}");

            if (PerformDataExchange(id, reqMsg, policy, (cbh) =>
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
            }) is byte[] currentData)
            {
                item.Data = currentData;
                return currentData;
            }
            else
                throw new InvalidDataException("Returned data are null");
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
        #endregion
    }
}
