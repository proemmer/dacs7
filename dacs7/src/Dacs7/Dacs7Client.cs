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
        private const ushort MaxParallelJobsDefault = 10;
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
                        return true;
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
                _clientSocket.OnConnectionStateChanged = OnClientStateChanged;
                _clientSocket.OnRawDataReceived = OnRawDataReceived;
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
                var itemSize = parameter.CalcSize(headerSize);
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
                            itemSize = tmpParam.CalcSize(headerSize);

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

        private int GetItemLength<T>(T parameter) where T : OperationParameter => parameter is WriteOperationParameter ? ItemWriteSlice : ItemReadSlice;

        private void SetupParameter<T>(int[] args, out ushort length, out ushort dbNr, out T policy) where T : S7ProtocolPolicy, new()
        {
            length = Convert.ToUInt16(args.Any() ? args[0] : 0);
            dbNr = Convert.ToUInt16(args.Length > 1 ? args[1] : 0);
            policy = new T();
        }

        #endregion
    }
}
