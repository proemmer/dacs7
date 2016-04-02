
using InacS7Core.Arch;
using InacS7Core.Communication;
using InacS7Core.Domain;
using InacS7Core.Helper;
using InacS7Core.Helper;
using InacS7Core.Helper.S7;
using InacS7Core.Protocols;
using InacS7Core.Protocols.RFC1006;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace InacS7Core
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class InacS7CoreClient : IInacS7CoreClient
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
        //private SemaphoreSlim _semaphore;
        private readonly UpperProtocolHandlerFactory _upperProtocolHandlerFactory = new UpperProtocolHandlerFactory();
        private readonly Queue<AutoResetEvent> _eventQueue = new Queue<AutoResetEvent>();
        private readonly ReaderWriterLockSlim _queueLockSlim = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _callbackLockSlim = new ReaderWriterLockSlim();
        private bool _disposed;
        private UInt16 _alarmUpdateId;
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
        private const UInt16 PduSizeDefault = 960;
        private int _referenceId;
        private readonly object _idLock = new object();
        private UInt16 _receivedPduSize = 0;
        #endregion

        #region Properties

        private UInt16 PduSize
        {
            get { return _receivedPduSize <= 0 ? _parameter.GetParameter("PduSize", PduSizeDefault) : _receivedPduSize; }
            set
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
        public UInt16 ReadItemMaxLength { get { return ItemReadSlice; } }

        /// <summary>
        /// Max bytes to write in one telegram.
        /// </summary>
        /// <returns></returns>
        public UInt16 WriteItemMaxLength { get { return ItemWriteSlice; } }

        /// <summary>
        /// Plc is connected or not.
        /// </summary>
        /// <returns></returns>
        public bool IsConnected { get { return _clientSocket != null && _clientSocket.IsConnected; } }

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

        ~InacS7CoreClient()
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

                if (IsConnected)
                    Disconnect();

                AssigneParameter();
                _clientSocket.OnConnectionStateChanged += OnClientStateChanged;
                _clientSocket.OnRawDataReceived += OnRawDataReceived;
                if (_clientSocket.Open())
                {
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
                        PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                            throw new InacS7Exception(errorClass, errorCode);
                        });
                    }
                    else
                        throw new TimeoutException("Timeout while waiting for connection confirmation");
                }
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

            //if (_semaphore != null)
            //{
            //    _semaphore.Dispose();
            //    _semaphore = null;
            //}
        }

        /// <summary>
        /// Disconnect from the plc asynchronous
        /// </summary>
        public Task DisconnectAsync()
        {
            return Task.Factory.StartNew(Disconnect, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Read data from the plc and convert it to the given .Net type.
        /// </summary>
        /// <param name="area">Specify the plc area to read.  e.g. IB InputByte</param>
        /// <param name="offset">Specify the read offset</param>
        /// <param name="type">Specify the .Net data type for the red data</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        public object ReadAny(PlcArea area, int offset, Type type, params int[] args)
        {
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
                Log(string.Format("ReadAny: ProtocolDataUnitReference is {0}", id));
                var currentData = PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                                var returnCode = cbh.ResponseMessage.GetAttribute(string.Format("Item[{0}].ItemReturnCode", i), (byte)0);
                                if (returnCode == 0xFF)
                                    result.Add(cbh.ResponseMessage.GetAttribute(string.Format("Item[{0}].ItemData", i), new byte[0]));
                                else
                                    throw new InacS7ReturnCodeException(returnCode, i);
                            }
                            return result;
                        }
                        var firstReturnCode = cbh.ResponseMessage.GetAttribute("Item[0].ItemReturnCode", (byte)0);
                        if (firstReturnCode == 0xFF)
                            return cbh.ResponseMessage.GetAttribute("Item[0].ItemData", new byte[0]);
                        throw new InacS7ContentException(firstReturnCode, 0);
                    }
                    var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                    throw new InacS7Exception(errorClass, errorCode);
                }) as byte[];

                if (currentData != null)
                    readResult.AddRange(currentData);
                else
                    throw new InvalidDataException("Returned data are null");
                packageLength -= ItemReadSlice;
            }
            return readResult.ToArray<byte>();
        }

        /// <summary>
        /// Read data from the plc as parallel and convert it to the given .Net type.
        /// </summary>
        /// <param name="area">Specify the plc area to read.  e.g. IB InputByte</param>
        /// <param name="offset">Specify the read offset</param>
        /// <param name="type">Specify the .Net data type for the red data</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        /// http://www.tugberkugurlu.com/archive/how-and-where-concurrent-asynchronous-io-with-asp-net-web-api
        private object ReadAnyPartsAsync(PlcArea area, int offset, Type type, params int[] args)
        {
            try
            {
                var length = Convert.ToUInt16(args.Any() ? args[0] : 0);
                var dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
                var packageLength = length;
                var readResult = new List<byte>();
                var requests = new List<Task<object>>();
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
        /// Read data from the plc as parallel and convert it to the given .Net type.
        /// </summary>
        /// <param name="area">Specify the plc area to read.  e.g. IB InputByte</param>
        /// <param name="offset">Specify the read offset</param>
        /// <param name="type">Specify the .Net data type for the red data</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        /// http://www.tugberkugurlu.com/archive/how-and-where-concurrent-asynchronous-io-with-asp-net-web-api
        public object ReadAnyParallel(PlcArea area, int offset, Type type, params int[] args)
        {
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
        /// Read data from the plc asynchronous and convert it to the given .Net type.
        /// </summary>
        /// <param name="area">Specify the plc area to read.  e.g. IB InputByte</param>
        /// <param name="offset">Specify the read offset</param>
        /// <param name="type">Specify the .Net data type for the red data</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        public Task<object> ReadAnyAsync(PlcArea area, int offset, Type type, params int[] args)
        {
            return Task.Factory.StartNew(() =>
                _maxParallelCalls <= 1 ?
                ReadAny(area, offset, type, args) :
                ReadAnyPartsAsync(area, offset, type, args),
                _taskCreationOptions);
        }

        /// <summary>
        /// Write data to the connected plc.
        /// </summary>
        /// <param name="area">Specify the plc area to write to.  e.g. OB OutputByte</param>
        /// <param name="offset">Specify the write offset</param>
        /// <param name="value">Value to write</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
        public void WriteAny(PlcArea area, int offset, object value, params int[] args)
        {
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
                Log(string.Format("WriteAny: ProtocolDataUnitReference is {0}", id));
                PerformeDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    var errorClass = cbh.ResponseMessage.GetAttribute("ErrorClass", (byte)0);
                    if (errorClass == 0x00)
                    {
                        var items = cbh.ResponseMessage.GetAttribute("ItemCount", (byte)0);
                        for (var i = 0; i < items; i++)
                        {
                            var returnCode = cbh.ResponseMessage.GetAttribute(string.Format("Item[{0}].ItemReturnCode", i), (byte)0);
                            if (returnCode != 0xff)
                                throw new InacS7ContentException(returnCode, i);
                        }
                        //all write operations are successfully
                        return;
                    }
                    var errorCode = cbh.ResponseMessage.GetAttribute("ErrorCode", (byte)0);
                    throw new InacS7Exception(errorClass, errorCode);
                });
                packageLength -= ItemWriteSlice;
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
        public void WriteAnyParallel(PlcArea area, int offset, object value, params int[] args)
        {

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
        /// Write data asynchronous to the connected plc.
        /// </summary>
        /// <param name="area">Specify the plc area to write to.  e.g. OB OutputByte</param>
        /// <param name="offset">Specify the write offset</param>
        /// <param name="value">Value to write</param>
        /// <param name="args">Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number </param>
        /// <returns></returns>
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
        /// Read the number of blocks in the plc per type
        /// </summary>
        /// <returns></returns>
        public IPlcBlocksCount GetBlocksCount()
        {
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateBlocksCountRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            Log(string.Format("GetBlocksCount: ProtocolDataUnitReference is {0}", id));
            return PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                    throw new InacS7ReturnCodeException(returnCode);
                }
                throw new InacS7ParameterException(errorCode);
            }) as IPlcBlocksCount;
        }

        /// <summary>
        /// Read the number of blocks in the plc per type asynchronous.
        /// </summary>
        /// <returns>Return a structure with all Counts of blocks</returns>
        public Task<IPlcBlocksCount> GetBlocksCountAsync()
        {
            return Task.Factory.StartNew(() => GetBlocksCount(), _taskCreationOptions);
        }

        /// <summary>
        /// Get all blocks of the specified type.
        /// </summary>
        /// <param name="type">Block type to read.</param>
        /// <returns>Return a list off all blocks of this type</returns>
        public IEnumerable<IPlcBlocks> GetBlocksOfType(PlcBlockType type)
        {
            var id = GetNextReferenceId();
            var policy = new S7UserDataProtocolPolicy();
            var blocks = new List<IPlcBlocks>();
            var lastUnit = false;
            var sequenceNumber = (byte)0x00;
            Log(string.Format("GetBlocksOfType: ProtocolDataUnitReference is {0}", id));

            do
            {
                var reqMsg = S7MessageCreator.CreateBlocksOfTypeRequest(id, type, sequenceNumber);
                var blocksPart = PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                                        Number = sslData.GetSwap<UInt16>(i),
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
                        throw new InacS7ReturnCodeException(returnCode);
                    }
                    throw new InacS7ParameterException(errorCode);
                }) as IEnumerable<IPlcBlocks>;

                if (blocksPart != null)
                    blocks.AddRange(blocksPart);
            } while (!lastUnit);
            return blocks;
        }

        /// <summary>
        /// Get all blocks of the specified type asynchronous.
        /// </summary>
        /// <param name="type">Block type to read.</param>
        /// <returns>Return a list off all blocks of this type</returns>
        public Task<IEnumerable<IPlcBlocks>> GetBlocksOfTypeAsync(PlcBlockType type)
        {
            return Task.Factory.StartNew(() => GetBlocksOfType(type), _taskCreationOptions);
        }

        /// <summary>
        /// Read the meta data of a block from the plc.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB</param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns></returns>
        public IPlcBlockInfo ReadBlockInfo(PlcBlockType blockType, int blocknumber)
        {
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateBlockInfoRequest(id, blockType, blocknumber);
            var policy = new S7UserDataProtocolPolicy();
            Log(string.Format("ReadBlockInfo: ProtocolDataUnitReference is {0}", id));
            return PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                    throw new InacS7ReturnCodeException(returnCode);
                }
                throw new InacS7ParameterException(errorCode);
            }) as IPlcBlockInfo;
        }

        /// <summary>
        /// Read the full data of a block from the plc.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB</param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns></returns>
        public byte[] UploadPlcBlock(PlcBlockType blockType, int blocknumber)
        {
            var id = GetNextReferenceId();

            var policy = new S7JobUploadProtocolPolicy();
            Log(string.Format("ReadBlockInfo: ProtocolDataUnitReference is {0}", id));

            //Start Upload
            var reqMsg = S7MessageCreator.CreateStartUploadRequest(id, blockType, blocknumber);
            uint controlId = 0;
            PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                throw new InacS7Exception(errorClass, errorCode);
            });

            //Upload packages
            reqMsg = S7MessageCreator.CreateUploadRequest(id, blockType, blocknumber, controlId);
            var data = new List<byte>();
            var hasNext = false;
            do
            {
                hasNext = false;
                PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                    throw new InacS7Exception(errorClass, errorCode);
                });
            } while (hasNext);


            reqMsg = S7MessageCreator.CreateEndUploadRequest(id, blockType, blocknumber, controlId);
            PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                throw new InacS7Exception(errorClass, errorCode);
            });

            return data.ToArray();
        }

        /// <summary>
        /// Write the full data of a block to the plc.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB</param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <param name="data">Plc block in byte</param>
        /// <returns></returns>
        public bool DownloadPlcBlock(PlcBlockType blockType, int blocknumber, byte[] data)
        {
            throw new NotImplementedException();
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateStartDownloadRequest(id, blockType, blocknumber, data);  //Start Download
            var policy = new S7UserDataProtocolPolicy();
            Log(string.Format("DownloadPlcBlock: ProtocolDataUnitReference is {0}", id));
            return (bool)PerformeDataExchange(id, reqMsg, policy, (cbh) =>
            {
                var errorCode = cbh.ResponseMessage.GetAttribute("ParamErrorCode", (ushort)0xff);
                if (errorCode == 0)
                {
                    var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
                    if (returnCode == 0x00)
                    {

                        //TODO!!!!!!!!!!!!!!!!!!!!!!
                        var callbackId = GetNextReferenceId();
                        var cbhOnUpdate = GetCallbackHandler(callbackId, true);
                        //_alarmUpdateId = callbackId;
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
                                            var subItemName = string.Format("Alarm[{0}].", 0) + "{0}";
                                            var isComing = msg.GetAttribute(string.Format(subItemName, "IsComing"), false);
                                            return;
                                        }
                                        throw new InvalidDataException("SSL Data are empty!");
                                    }
                                    throw new InacS7ReturnCodeException(returnCodeCb);
                                }
                                catch (Exception ex)
                                {
                                    Log(ex.Message);
                                }
                            }
                            else if (cbhOnUpdate.OccuredException != null)
                            {
                                Log(cbhOnUpdate.OccuredException.Message);
                            }
                        };
                        return callbackId;
                    }
                    throw new InacS7ReturnCodeException(returnCode);
                }
                throw new InacS7ParameterException(errorCode);
            });
        }

        /// <summary>
        /// Read the meta data of a block asynchronous from the plc.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB</param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns></returns>
        public Task<IPlcBlockInfo> ReadBlockInfoAsync(PlcBlockType blockType, int blocknumber)
        {
            return Task.Factory.StartNew(() => ReadBlockInfo(blockType, blocknumber), _taskCreationOptions);
        }

        /// <summary>
        /// Read the current pending alarms from the plc.
        /// </summary>
        /// <returns>returns a list of all pending alarms</returns>
        public IEnumerable<IPlcAlarm> ReadPendingAlarms()
        {
            var id = GetNextReferenceId();
            var policy = new S7UserDataProtocolPolicy();
            var alarms = new List<IPlcAlarm>();
            var lastUnit = false;
            var sequenceNumber = (byte)0x00;
            Log(string.Format("ReadBlockInfo: ProtocolDataUnitReference is {0}", id));

            do
            {
                var reqMsg = S7MessageCreator.CreatePendingAlarmRequest(id, sequenceNumber);
                var alarmPart = PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                                var subItemName = string.Format("Alarm[{0}].", i) + "{0}";
                                var isComing = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "IsComing"), false);
                                var isAck = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "IsAck"), false);
                                var ack = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "Ack"), false);
                                result.Add(new PlcAlarm
                                {
                                    Id = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "Id"), (UInt16)0),
                                    MsgNumber = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "MsgNumber"), (UInt32)0),
                                    IsComing = isComing,
                                    IsAck = isAck,
                                    Ack = ack,
                                    AlarmSource = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "AlarmSource"), (UInt16)0),
                                    Timestamp = ExtractTimestamp(cbh.ResponseMessage, i, !isComing && !isAck && ack ? 1 : 0),
                                    AssotiatedValue = ExtractAssotiatedValue(cbh.ResponseMessage, i)
                                });
                            }

                            lastUnit = cbh.ResponseMessage.GetAttribute("LastDataUnit", true);
                            sequenceNumber = cbh.ResponseMessage.GetAttribute("SequenceNumber", (byte)0x00);

                            return result;
                        }
                        throw new InacS7ReturnCodeException(returnCode);
                    }
                    throw new InacS7ParameterException(errorCode);
                }) as IEnumerable<IPlcAlarm>;

                if (alarmPart != null)
                    alarms.AddRange(alarmPart);
            } while (!lastUnit);
            return alarms;
        }

        /// <summary>
        /// Read the current pending alarms asynchronous from the plc.
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

            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateAlarmCallbackRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            Log(string.Format("RegisterAlarmUpdateCallback: ProtocolDataUnitReference is {0}", id));
            return (ushort)PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                                            var subItemName = string.Format("Alarm[{0}].", 0) + "{0}";
                                            var isComing = msg.GetAttribute(string.Format(subItemName, "IsComing"), false);
                                            onAlarmUpdate(new PlcAlarm
                                            {
                                                Id = msg.GetAttribute(string.Format(subItemName, "Id"), (UInt16)0),
                                                MsgNumber = msg.GetAttribute(string.Format(subItemName, "MsgNumber"), (UInt32)0),
                                                IsComing = isComing,
                                                IsAck = msg.GetAttribute(string.Format(subItemName, "IsAck"), false),
                                                Ack = msg.GetAttribute(string.Format(subItemName, "Ack"), false),
                                                AlarmSource = msg.GetAttribute(string.Format(subItemName, "AlarmSource"), (UInt16)0),
                                                Timestamp = ExtractTimestamp(msg, 0),
                                                AssotiatedValue = ExtractAssotiatedValue(msg, 0)
                                            });
                                            return;
                                        }
                                        throw new InvalidDataException("SSL Data are empty!");
                                    }
                                    throw new InacS7ReturnCodeException(returnCodeCb);
                                }
                                catch (Exception ex)
                                {
                                    Log(ex.Message);
                                    if (onErrorOccured != null)
                                        onErrorOccured(ex);
                                }
                            }
                            else if (cbhOnUpdate.OccuredException != null)
                            {
                                Log(cbhOnUpdate.OccuredException.Message);
                                if (onErrorOccured != null)
                                    onErrorOccured(cbhOnUpdate.OccuredException);
                            }
                        };
                        return callbackId;
                    }
                    throw new InacS7ReturnCodeException(returnCode);
                }
                throw new InacS7ParameterException(errorCode);
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
        /// Read the number of blocks in the plc per type
        /// </summary>
        /// <returns></returns>
        public DateTime GetPlcTime()
        {
            var id = GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateReadClockRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            Log(string.Format("GetPlcTime: ProtocolDataUnitReference is {0}", id));
            return (DateTime)PerformeDataExchange(id, reqMsg, policy, (cbh) =>
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
                            return ConvertToDateTime(sslData);
                        }
                        throw new InvalidDataException("SSL Data are empty!");
                    }
                    throw new InacS7ReturnCodeException(returnCode);
                }
                throw new InacS7ParameterException(errorCode);
            });
        }

        /// <summary>
        /// Read the number of blocks in the plc per type
        /// </summary>
        /// <returns></returns>
        public Task<DateTime> GetPlcTimeAsync()
        {
            return Task.Factory.StartNew(() => GetPlcTime(), _taskCreationOptions);
        }


        #region Helper

        private void OnClientStateChanged(string socketHandle, bool connected)
        {
            if (connected)
            {
                _upperProtocolHandlerFactory.OnConnected();
            }
            Log(string.Format("OnClientStateChanged to {0}.", connected ? "connected" : "disconnected"));
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
                        Log(string.Format("OnRawDataReceived: Received Data size was {0}", array.Length));
                        var policy = GetProtocolPolicy(array);
                        if (policy != null)
                        {
                            Log(string.Format("OnRawDataReceived: determined policy is {0}", policy.GetType().Name));
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
                                    CallbackHandler cb;
                                    if (_callbacks.TryGetValue(id, out cb))
                                    {
                                        cb.ResponseMessage = msg;

                                        if (cb.Event != null)
                                            cb.Event.Set();
                                        else if (cb.OnCallbackAction != null)
                                            cb.OnCallbackAction(cb.ResponseMessage);
                                    }
                                    else
                                    {
                                        Log(string.Format("OnRawDataReceived: message with id {0} has no waiter!", id));
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
                Log(string.Format("OnRawDataReceived: Exception was {0} -{1}", ex.Message, ex.StackTrace));
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
                    else if (cb.OnCallbackAction != null)
                        cb.OnCallbackAction(null);
                }

            }
        }

        private void AssigneParameter()
        {
            _timeout = _parameter.GetParameter("Receive Timeout", 5000);
            _maxParallelJobs = _parameter.GetParameter("Maximum Parallel Jobs", (ushort)1);  //Used by simatic manager -> best performance with 1
            _maxParallelCalls = _parameter.GetParameter("Maximum Parallel Calls", (ushort)4); //Used by InacS7
            _taskCreationOptions = _parameter.GetParameter("Use Threads", true) ? TaskCreationOptions.LongRunning : TaskCreationOptions.None; //Used by InacS7
            //_semaphore = new SemaphoreSlim(_maxParallelCalls, _maxParallelCalls);
            _sleeptimeAfterMaxPendingCallsReached = _parameter.GetParameter("Sleeptime After Max Pending Calls Reached", 5);

            var config = new ClientSocketConfiguration
            {
                Hostname = _parameter.GetParameter("Ip", "127.0.0.1"),
                ServiceName = _parameter.GetParameter("Port", 102),
                ReceiveBufferSize = _parameter.GetParameter("ReceiveBufferSize", 65536)
                //                _parameter.GetParameter("Reconnect", false),
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
            if (OnLogEntry != null)
                OnLogEntry(message);
        }

        private object PerformeDataExchange(ushort id, IMessage msg, IProtocolPolicy policy, Func<CallbackHandler, object> func)
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
                    throw new TimeoutException("Timeout while waiting for Response.");
            }
            finally
            {
                ReleaseCallbackHandler(id);
            }
        }

        private void PerformeDataExchange(ushort id, IMessage msg, IProtocolPolicy policy, Action<CallbackHandler> action)
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
                    throw new Exception("No Response message was been created!");
                }
                else
                    throw new TimeoutException("Timeout while waiting for Response.");
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
                    Log(String.Format("Number of queued events {0}", _eventQueue.Count));
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
            var subItemName = string.Format("Alarm[{0}].ExtendedData[{1}].", alarmindex, 0) + "{0}";

            if (msg.GetAttribute(string.Format(subItemName, "NumberOfAssotiatedValues"), 0) > 0)
            {
                return msg.GetAttribute(string.Format(subItemName, "AssotiatedValue"), new byte[0]);
            }
            return new byte[0];
        }

        private static DateTime ExtractTimestamp(IMessage msg, int alarmindex, int tsIdx = 0)
        {
            var subItemName = string.Format("Alarm[{0}].ExtendedData[{1}].", alarmindex, tsIdx) + "{0}";
            return msg.GetAttribute(string.Format(subItemName, "Timestamp"), DateTime.MinValue);
        }

        private static object ConvertTo<T>(byte[] data, T instance, int length, int offset = 0)
        {
            if (length == 0)
            {
                if (instance is bool)
                    return data[0] == 0x01;

                if (instance is byte)
                    return data[0];

                return data;
            }

            const int typeSize = 1;
            var result = new List<T>();
            for (var i = 0; i < data.Length; i += typeSize)
                result.Add((T)ConvertTo(data, instance, 0, i));
            return result.ToArray<T>();
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

        private static DateTime ConvertToDateTime(IList<byte> data, int offset = 2)
        {
            if (data == null || !data.Any())
                return new DateTime(1900, 01, 01, 00, 00, 00);

            int bt = data[offset];
            //BCD
            bt = (((bt >> 4)) * 10) + ((bt & 0x0f));
            var jahr = bt < 90 ? 2000 : 1900;
            jahr += bt;

            //month
            bt = data[offset + 1];
            var monat = (((bt >> 4)) * 10) + ((bt & 0x0f));

            //day
            bt = data[offset + 2];
            var tag = (((bt >> 4)) * 10) + ((bt & 0x0f));

            //hour
            bt = data[offset + 3];
            var stunde = (((bt >> 4)) * 10) + ((bt & 0x0f));

            //minutes
            bt = data[offset + 4];
            var minute = (((bt >> 4)) * 10) + ((bt & 0x0f));

            //second
            bt = data[offset + 5];
            var sekunde = (((bt >> 4)) * 10) + ((bt & 0x0f));

            //millisecond
            //Byte 6 BCD + MSB (Byte 7)
            bt = data[offset + 6];
            int bt1 = data[offset + 7];
            var mili = (((bt >> 4)) * 10) + ((bt & 0x0f));
            mili = mili * 10 + (bt1 >> 4);

            //week day
            //LSB (Byte 7) 1=Sunday
            //bt = b[pos + 7];
            //week day = (bt1 & 0x0f); 
            try
            {
                return new DateTime(jahr, monat, tag, stunde, minute, sekunde, mili);
            }
            catch (Exception)
            {
                return new DateTime(1900, 01, 01, 00, 00, 00);
            }
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
            T result;
            if (Enum.TryParse(s, out result))
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
                var enumAttributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
                if (enumAttributes != null && enumAttributes.Length > 0)
                    return enumAttributes[0].Description;
            }
            return e.ToString();
        }
        #endregion
    }
}
