using Dacs7.Domain;
using Dacs7.Helper;
using Dacs7.Protocols.S7;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {


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




        internal static int CalculateSizeForGenericWriteOperation<T>(PlcArea area, T value, int length, out Type elementType)
        {
            elementType = null;
            if (value is Array && length < 0)
            {
                elementType = typeof(T).GetElementType();
                length = (value as Array).Length * TransportSizeHelper.DataTypeToSizeByte(elementType, area);
            }
            var size = length < 0 ? TransportSizeHelper.DataTypeToSizeByte(typeof(T), PlcArea.DB) : length;
            if (value is string stringValue)
            {
                if (length < 0) size = stringValue.Length;
                size += 2;
            }
            return size;
        }
    }
}
