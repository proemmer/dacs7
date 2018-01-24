using Dacs7.Domain;
using Dacs7.Helper;
using Dacs7.Protocols.S7;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {

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
            SetupGenericReadData<T>(ref offset, out Type readType, out int bytesToRead);
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
            return _maxParallelCalls <= 1 ?
                Task.Factory.StartNew(() => ReadAny(area, offset, type, args), _taskCreationOptions) :
                ReadAnyPartsAsync(area, offset, type, args);
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

        public static object InvokeGenericMethod<T>(Type genericType, string methodName, object[] parameters)
        {
            var method = parameters.All(x => x != null)
                                ? typeof(T).GetMethod(methodName, parameters.Select(x => x.GetType()).ToArray())
                                : typeof(T).GetMethod(methodName);
            var genericMethod = method.MakeGenericMethod(genericType);
            return genericMethod.Invoke(null, parameters);
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

        private static void SetupGenericReadData<T>(ref int offset, out Type readType, out int bytesToRead)
        {
            SetupGenericReadData<T>(ref offset, out readType, out bytesToRead, out _, out _, out _, out _, out _);
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


        private IEnumerable<T> ConvertToEnumerable<T>(int numberOfItems, Type t, bool isBool, bool isString, int elementLength, int bitOffset, int bytesToRead, byte[] data)
        {
            var result = ConvertTo<T>(numberOfItems, t, isBool, isString, elementLength, bitOffset, bytesToRead, data);
            if (result is IEnumerable<T>)
            {
                return (IEnumerable<T>)result;
            }
            var resultList = new List<T>
            {
                (T)result
            };
            return resultList;
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



    }
}
