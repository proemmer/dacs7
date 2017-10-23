using Dacs7.Domain;
using Dacs7.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dacs7
{
    internal static class S7MessageCreator
    {
        /// <summary>
        /// This constructor search all occurrences of IProtocolPolicy and 
        /// creates an instance of it to register them in the Policy factory
        /// </summary>
        static S7MessageCreator()
        {
            try
            {
                var type = typeof(IProtocolPolicy);
                var assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    foreach (var t in assembly.GetReferencedAssemblies()
                        .Select(info => Assembly.Load(new AssemblyName(info.Name)))
                        .SelectMany(s => s.GetTypes())
                        .Where(type.IsAssignableFrom))
                    {
                        try
                        {
                            Activator.CreateInstance(t);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Fill the communication header attributes with the given variables
        /// </summary>
        /// <param name="message">message to assign</param>
        /// <param name="pduType">type of S7Pdu  e.g. Job</param>
        /// <param name="lengthOfPayload">number of bytes after the header</param>
        /// <param name="lengthOfParam">number of bytes for parameters</param>
        /// <param name="duRef">ProtocolDataUnitReference is used to find correct pending message to an ack</param>
        /// <param name="identifier"></param>
        /// <param name="protocolId">Most of the time  0x32</param>
        private static void FillCommHeader(IMessage message, byte pduType, ushort lengthOfPayload = 0, ushort lengthOfParam = 4, ushort duRef = 0, ushort identifier = 0, byte protocolId = 0x32 )
        {
            message.SetAttribute("ProtocolId", protocolId);
            message.SetAttribute("PduType", pduType);
            message.SetAttribute("RedundancyIdentification", identifier);
            message.SetAttribute("ProtocolDataUnitReference", duRef);
            message.SetAttribute("ParamLength", lengthOfParam);
            message.SetAttribute("DataLength", lengthOfPayload);
        }

        /// <summary>
        /// Add parameter for user data datagram.
        /// </summary>
        /// <param name="message">message to assign to</param>
        /// <param name="length"></param>
        /// <param name="type"></param>
        /// <param name="group"></param>
        /// <param name="subfunction"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="reference"></param>
        /// <param name="lastDataUnit"></param>
        private static void AddUserDataParameter(IMessage message, byte length, UserDataFunctionType type, UserDataFunctionGroup group , byte subfunction, byte sequenceNumber = 0, byte reference = 0, bool lastDataUnit = true)
        {
            message.SetAttribute("ParamHeader",new byte[]
            {
                0x00,
                0x01,
                0x12
            });

            message.SetAttribute("ParamDataLength", length);
            message.SetAttribute("Unknown", length == 4 ? (byte)0x11 : (byte)0x12);
            message.SetAttribute("TypeAndGroup", (byte)(((byte)type << 4) | (byte)group));
            message.SetAttribute("SubFunction", subfunction);
            message.SetAttribute("SequenceNumber", sequenceNumber);

            if (length == 8)
            {
                message.SetAttribute("DataUnitReferenceNumber", reference);
                message.SetAttribute("LastDataUnit", lastDataUnit ? (byte)0 : (byte)1);
                message.SetAttribute("ErrorCode", (ushort)0);
            }
        }

        /// <summary>
        /// Add parameter for job datagram.
        /// </summary>
        /// <param name="message">Message on which the parameter are set</param>
        /// <param name="function">Function to set</param>
        /// <param name="parallelJobs">Number of parallel jobs handled by the plc</param>
        /// <param name="length">PDU length</param>
        private static void AddJobParameter(IMessage message, byte function, ushort parallelJobs, ushort length)
        {
            message.SetAttribute("Function", function);
            message.SetAttribute("Reserved", (byte)0x00);
            message.SetAttribute("MaxAmQCalling", parallelJobs);
            message.SetAttribute("MaxAmQCalled", parallelJobs);
            message.SetAttribute("PduLength", length);
        }

        private static void AddInvocationParameter(IMessage message, byte function, string pi, byte[] parameterExt = null)
        {
            message.SetAttribute("Function", function);
            var res = new List<byte> { 0x00, 0x00, 0x00, 0x00, 0x00 };
            if(parameterExt != null)
            {
                res.AddRange(parameterExt);
            }
            message.SetAttribute("Reserved", res.ToArray());
            message.SetAttribute("LengthPart2", (byte)pi.Length);
            message.SetAttribute("Pi", Encoding.ASCII.GetBytes(pi));
        }


        /// <summary>
        /// Add parameter for upload datagram.
        /// </summary>
        /// <param name="message">Message on which the parameter are set</param>
        /// <param name="function">Function to set</param>
        /// <param name="blockType"><see cref="PlcBlockType"/></param>
        /// <param name="blockNumber">number of plc block</param>
        /// <param name="length">LengthPart1</param>
        /// <param name="controlId">Reserved2</param>
        private static void AddUploadParameter(IMessage message, byte function, PlcBlockType blockType, int blockNumber, byte length = 0, uint controlId = 0)
        {
            message.SetAttribute("Function", function);
            message.SetAttribute("Reserved", (byte)0x00);
            message.SetAttribute("ErrorCode", (ushort)0x0000);
            message.SetAttribute("Reserved2", controlId);
            if (function == 0x1d)
            {
                message.SetAttribute("LengthPart1", length);
                message.SetAttribute("FileIdentifier", (byte)0x5f);
                message.SetAttribute("Unknown1", (byte)0x30);
                message.SetAttribute("BlockType", (byte)blockType);
                message.SetAttribute("BlockNumber", Encoding.ASCII.GetBytes(string.Format("{0:00000}", blockNumber)));
                message.SetAttribute("DestFilesystem", (byte)0x41);
            }
        }

        /// <summary>
        /// Add parameters for download datagram
        /// </summary>
        /// <param name="message">Message on which the parameter are set</param>
        /// <param name="function">Function to set</param>
        /// <param name="blockType"><see cref="PlcBlockType"/></param>
        /// <param name="blockNumber">number of plc block</param>
        /// <param name="loadMemSize">LoadMemLength</param>
        /// <param name="Mc7Size">Size of mc7Code</param>
        /// <param name="lastDu">Is last data unit</param>
        private static void AddDownloadParameter(IMessage message, byte function, PlcBlockType blockType, int blockNumber, int loadMemSize, int Mc7Size, bool lastDu = false)
        {
            message.SetAttribute("Function", function);
            message.SetAttribute("Reserved", lastDu ? (byte)0x01 : (byte)0x00);

            if (function == 0x1a)
            {
                message.SetAttribute("ErrorCode", (ushort)0x0100);
                message.SetAttribute("Reserved2", (uint)0);

                message.SetAttribute("LengthPart1", 9);
                message.SetAttribute("FileIdentifier", (byte)0x5f);
                message.SetAttribute("Unknown1", (byte)0x30);
                message.SetAttribute("BlockType", (byte)blockType);
                message.SetAttribute("BlockNumber", Encoding.ASCII.GetBytes(string.Format("{0:00000}", blockNumber)));
                message.SetAttribute("DestFilesystem", (byte)0x50);


                message.SetAttribute("LengthPart2", 13);
                message.SetAttribute("Unknown2", (byte)0x31);
                message.SetAttribute("LoadMemLength", Encoding.ASCII.GetBytes(string.Format("{0:00000}", loadMemSize)));
                message.SetAttribute("MC7Length", Encoding.ASCII.GetBytes(string.Format("{0:00000}", Mc7Size)));
            }
        }


        private static void AddReadWriteParameter(IMessage message, byte function, byte itemCount)
        {
            message.SetAttribute("Function", function);
            message.SetAttribute("ItemCount", itemCount);
        }

        private static void AddData(IMessage message, byte[] data)
        {
            message.SetAttribute("ReturnCode", data.Length > 0 ? (byte)ItemResponseRetValue.DataOk : (byte)ItemResponseRetValue.DataError);
            message.SetAttribute("TransportSize", data.Length > 0 ? (byte)DataTransportSize.OctetString : (byte)DataTransportSize.Null);
            message.SetAttribute("UserDataLength", (ushort)data.Length);
            message.SetAttribute("SSLData", data);
        }

        public static IMessage CreateCommunicationSetup(ushort unitId, ushort maxParallelJobs, ushort pduSize)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.Job, 0, 8, unitId);
            AddJobParameter(msg, (byte)FunctionCode.SetupComm, maxParallelJobs , pduSize);
            return msg;
         }

        public static IMessage CreateReadRequest(ushort unitId, PlcArea area ,ushort dbnr, int offset, ushort length, Type t)
        {
            var msg = Message.Create();
            var isBit = t == typeof(bool);
            FillCommHeader(msg, (byte)PduType.Job, 0, 14, unitId);
            AddReadWriteParameter(msg, (byte)FunctionCode.ReadVar, 1);
            for (var i = 0; i < 1; i++)
            {
                var size = isBit || area == PlcArea.CT || area == PlcArea.TM ? (byte)ItemDataTransportSize.Bit : (byte)ItemDataTransportSize.Byte;

                var prefix = $"Item[{i}].";
                msg.SetAttribute(prefix + "VariableSpecification", (byte)0x12);
                const byte specLength = 0x0a;
                msg.SetAttribute(prefix + "LengthOfAddressSpecification", specLength);
                msg.SetAttribute(prefix + "SyntaxId", (byte)ItemSyntaxId.S7Any);
                msg.SetAttribute(prefix + "TransportSize", (byte)size);
                msg.SetAttribute(prefix + "ItemSpecLength", length);
                msg.SetAttribute(prefix + "DbNumber", dbnr);
                msg.SetAttribute(prefix + "Area", area);


                offset = isBit ? offset : (offset * 8);
                var address = new byte[3];
                address[2] = (byte)(offset & 0x000000FF);
                offset = offset >> 8;
                address[1] = (byte)(offset & 0x000000FF);
                offset = offset >> 8;
                address[0] = (byte)(offset & 0x000000FF);

                msg.SetAttribute(prefix + "Address", address);
                offset += specLength + 2;
            }

            return msg;
        }

        public static IMessage CreateReadRequests(ushort unitId, IEnumerable<ReadOperationParameter> parameters)
        {
            var msg = Message.Create();
            var numOfitems = parameters.Count();

            FillCommHeader(msg, (byte)PduType.Job, 0, (ushort)(2 + numOfitems * 12), unitId);
            AddReadWriteParameter(msg, (byte)FunctionCode.ReadVar, (byte)numOfitems);
            var index = 0;
            foreach (var opParam in parameters)
            {
                CreateAddressSpecification(msg, index, opParam);
                index++;
            }

            return msg;
        }

        //TODO:  Handle array of bool correct!!!!
        public static IMessage CreateWriteRequest(ushort unitId, PlcArea area, ushort dbnr, int offset, ushort length, object data)
        {
            var msg = Message.Create();
            var isArray = data is Array;
            var isBool = isArray ? (data as Array).GetValue(0) is bool : data is bool;  // Handle Array of bools
            var numberOfItems = !isBool ? 1 : length;
            var itemLength = isBool ? (ushort)1 : length;
            var payloadLength = (ushort)(!isBool ? length + 4 : length * 6 - 1); //=we need a fillbyte between each item has to be a fill byte if the length is odd.
            var paramLength = (ushort)(!isBool ? 14 : 2 + 12 * numberOfItems);

            FillCommHeader(msg, (byte)PduType.Job, payloadLength, paramLength, unitId);
            AddReadWriteParameter(msg, (byte)FunctionCode.WriteVar, Convert.ToByte(numberOfItems));
            var t = data.GetType();
            var enumerable = ConvertDataToByteArray(data);
            var typeLength = TransportSizeHelper.DataTypeToSizeByte(t, area);

            for (var i = 0; i < numberOfItems; i++)
            {
                var size = isBool || area == PlcArea.CT || area == PlcArea.TM ? (byte)ItemDataTransportSize.Bit : (byte)ItemDataTransportSize.Byte;
                var addr = i * typeLength + offset;

                var prefix = $"Item[{i}].";
                msg.SetAttribute(prefix + "VariableSpecification", (byte)0x12);
                const byte specLength = 0x0a;
                msg.SetAttribute(prefix + "LengthOfAddressSpecification", specLength);
                msg.SetAttribute(prefix + "SyntaxId", (byte)ItemSyntaxId.S7Any);
                msg.SetAttribute(prefix + "TransportSize", (byte)size);
                msg.SetAttribute(prefix + "ItemSpecLength", itemLength);
                msg.SetAttribute(prefix + "DbNumber", dbnr);
                msg.SetAttribute(prefix + "Area", area);

                var offsetAddress = size == 0x01 ? addr : addr * 8;
                var address = new byte[3];
                address[2] = (byte)(offsetAddress & 0x000000FF);
                offsetAddress = offsetAddress >> 8;
                address[1] = (byte)(offsetAddress & 0x000000FF);
                offsetAddress = offsetAddress >> 8;
                address[0] = (byte)(offsetAddress & 0x000000FF);

                msg.SetAttribute(prefix + "Address", address);
            }


            for (var i = 0; i < numberOfItems; i++)
            { 
                var prefix = $"DataItem[{i}].";
                msg.SetAttribute(prefix + "ItemDataReturnCode", (byte)0x00);      
                msg.SetAttribute(prefix + "ItemDataTransportSize", isBool ? (byte)DataTransportSize.Bit : (byte)DataTransportSize.Byte);
                msg.SetAttribute(prefix + "ItemDataLength", itemLength);
                msg.SetAttribute(prefix + "ItemData", isBool ? new byte[] { enumerable[i] } : enumerable);
            }

            return msg;
        }

        public static IMessage CreateWriteRequests(ushort unitId, IEnumerable<WriteOperationParameter> parameters)
        {
            var msg = Message.Create();
            var numOfitems = parameters.Count();
            var fullLength = 0;

            foreach (var item in parameters)
            {
                if ((fullLength % 2) != 0)  fullLength++;
                fullLength += item.Length;

                //Add string meta data max and current to the length
                if (item.Type == typeof(string)) fullLength += 2;
            }


            FillCommHeader(msg, (byte)PduType.Job, (ushort)(fullLength + numOfitems * 4), (ushort)(2 + numOfitems * 12), unitId);
            AddReadWriteParameter(msg, (byte)FunctionCode.WriteVar, (byte)numOfitems);


            var index = 0;
            foreach (var opParam in parameters)
            {
                CreateAddressSpecification(msg, index, opParam);
                index++;
            }

            index = 0;
            foreach (var opParam in parameters)
            {
                var size = opParam.Data is bool ? (byte)DataTransportSize.Bit : (byte)DataTransportSize.Byte;
                var prefix = string.Format("DataItem[{0}].", index);
                msg.SetAttribute(prefix + "ItemDataReturnCode", (byte)0x00);

                var data = ConvertDataToByteArray(opParam.Data);
                msg.SetAttribute(prefix + "ItemDataTransportSize", size);
                msg.SetAttribute(prefix + "ItemDataLength", (ushort)data.Length);
                msg.SetAttribute(prefix + "ItemData", data);

                index++;
            }

            return msg;
        }

        public static IMessage CreateBlockInfoRequest(ushort unitId, PlcBlockType blockType, int blockNumber)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.UserData, 12, 8, unitId);
            AddUserDataParameter(msg, 4, UserDataFunctionType.Request, UserDataFunctionGroup.Block, (byte)UserDataSubFunctionBlock.BlockInfo);

            var b = new List<byte> {0x30, (byte) blockType};
            b.AddRange(Encoding.ASCII.GetBytes(string.Format("{0:00000}", blockNumber)));
            b.Add(0x41);

            AddData(msg, b.ToArray());
            return msg;
        }

        public static IMessage CreateStartDownloadRequest(ushort unitId, PlcBlockType blockType, int blockNumber, byte[] data)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.Job, 0, 32, unitId);
            AddDownloadParameter(msg, (byte)FunctionCode.RequestDownload, blockType, blockNumber, 9,0);
            return msg;
        }

        public static IMessage CreateStartUploadRequest(ushort unitId, PlcBlockType blockType, int blockNumber)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.Job, 0, 18, unitId);
            AddUploadParameter(msg, (byte)FunctionCode.StartUpload, blockType, blockNumber,9);
            return msg;
        }

        public static IMessage CreateUploadRequest(ushort unitId, PlcBlockType blockType, int blockNumber, uint controlId)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.Job, 0, 8, unitId);
            AddUploadParameter(msg, (byte)FunctionCode.Upload, blockType, blockNumber, 0, controlId);
            return msg;
        }

        public static IMessage CreateEndUploadRequest(ushort unitId, PlcBlockType blockType, int blockNumber, uint controlId)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.Job, 0, 8, unitId);
            AddUploadParameter(msg, (byte)FunctionCode.EndUpload, blockType, blockNumber, 0, controlId);
            return msg;
        }

        public static IMessage CreatePendingAlarmRequest(ushort unitId, byte sequenceNumber = 0)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.UserData, sequenceNumber == 0 ? (ushort)16 : (ushort)4, sequenceNumber == 0 ? (ushort)8 : (ushort)12, unitId);
            AddUserDataParameter(msg, sequenceNumber == 0x00 ? (byte)4 : (byte)8, UserDataFunctionType.Request, UserDataFunctionGroup.Cpu,(byte)UserDataSubFunctionCpu.AlarmInit, sequenceNumber);
            AddData(msg, sequenceNumber == 0 ? new Byte[] {0x00, 0x01, 0x12, 0x08, 0x1a, 0x00, 0x01, 0x34, 0x00, 0x00, 0x00, 0x04} : new Byte[0]);
            return msg;
        }

        public static IMessage CreateAlarmCallbackRequest(ushort unitId, bool activate = true)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.UserData, 16, 8, unitId);
            AddUserDataParameter(msg, 4, UserDataFunctionType.Request, UserDataFunctionGroup.Cpu, (byte)UserDataSubFunctionCpu.Msgs);
            AddData(msg, new Byte[] { 0x86, 0x00, 0x61, 0x73, 0x6d, 0x65, 0x73, 0x73, 0x00, 0x00, activate ? (byte)0x09 : (byte)0x08, 0x00 });
            return msg;
        }

        public static IMessage CreateBlocksCountRequest(ushort unitId)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.UserData, 4, 8, unitId);
            AddUserDataParameter(msg, 4, UserDataFunctionType.Request, UserDataFunctionGroup.Block, (byte)UserDataSubFunctionBlock.List);

            AddData(msg, new byte[0]);
            return msg;
        }

        public static IMessage CreateBlocksOfTypeRequest(ushort unitId, PlcBlockType blockType, byte sequenceNumber = 0)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.UserData, sequenceNumber == 0x00 ? (ushort)6 : (ushort)4, sequenceNumber == 0x00 ? (ushort)8 : (ushort)12, unitId);
            AddUserDataParameter(msg, sequenceNumber == 0x00 ? (byte)4 : (byte)8, UserDataFunctionType.Request, UserDataFunctionGroup.Block, (byte)UserDataSubFunctionBlock.ListType, sequenceNumber);
            AddData(msg, sequenceNumber == 0 ?
                new byte[]
                {
                    0x30,  // ??
                    (byte)blockType //Block Type
                } :
                new byte[0]);
            return msg;
        }

        public static IMessage CreateReadClockRequest(ushort unitId)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.UserData, 4, 8, unitId);
            AddUserDataParameter(msg, 4, UserDataFunctionType.Request, UserDataFunctionGroup.Time, (byte)UserDataSubFunctionTime.Read);

            AddData(msg, new byte[0]);
            return msg;
        }

        public static IMessage CreateWriteClockRequest(ushort unitId, byte[] data)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.UserData, 14, 8, unitId);
            AddUserDataParameter(msg, 4, UserDataFunctionType.Request, UserDataFunctionGroup.Time, (byte)UserDataSubFunctionTime.Set);
            AddData(msg, data);
            return msg;
        }

        public static IMessage CreatePlcStopRequest(ushort unitId)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.Job, 0, 16, unitId);
            AddInvocationParameter(msg, (byte)FunctionCode.PlcStop, "P_PROGRAM");
            return msg;
        }

        public static IMessage CreateInvocationRequest(ushort unitId, string pi, string parameterblock = "")
        {
            var msg = Message.Create();
            var length = (ushort)11;
            length += (ushort)parameterblock.Length;
            length += (ushort)pi.Length;
            FillCommHeader(msg, (byte)PduType.Job, 0, length, unitId);
            var data = new List<byte>();
            data.AddRange(new byte[] { 0x00, 0xfd });
            // size
            data.AddRange(((ushort)parameterblock.Length).SetSwap());
            // parameter block
            data.AddRange(Encoding.ASCII.GetBytes(parameterblock));
            
            //else
            //{
            //    data.AddRange(new byte[] { 0x00, 0xfd, 0x00, 0x02, 0x43, 0x20 });
            //}
            AddInvocationParameter(msg, (byte)FunctionCode.PlcControl, pi, data.ToArray());
            return msg;
        }




        public static IMessage CreateSllRequest(ushort unitId, ushort sslId, ushort sslIndex)
        {
            var msg = Message.Create();
            FillCommHeader(msg, (byte)PduType.UserData, 8, 8, unitId);
            AddUserDataParameter(msg, 4, UserDataFunctionType.Request, UserDataFunctionGroup.Cpu, (byte)UserDataSubFunctionCpu.ReadSsl);
            var data = new List<byte>();
            data.AddRange(sslId.SetSwap());
            data.AddRange(sslIndex.SetSwap());
            AddData(msg, data.ToArray());
            return msg;
        }

        private static byte[] ConvertDataToByteArray(object data)
        {
            var enumerable = data as byte[];
            if (enumerable == null)
            {
                var boolEnum = data as bool[];
                if (boolEnum == null)
                {
                    if (data is bool)
                        return new byte[] {(bool) data ? (byte) 0x01 : (byte) 0x00};
                    if (data is byte || data is char)
                        return new byte[] {(byte) data};
                    var result = Converter.SetSwap(data);
                    if (result != null)
                        return result;
                    if (data is char[] charEnum)
                        return charEnum.Select(x => Convert.ToByte(x)).ToArray();
                    if (data is string stringEnum)
                        return (new byte[] { (byte)stringEnum.Length, (byte)stringEnum.Length }).Concat(stringEnum.ToCharArray().Select(x => Convert.ToByte(x)).ToArray());
                    if (data is Array)
                    {
                        var resultL = new List<byte>();
                        var arr = data as Array;
                        foreach (var item in arr)
                        {
                            resultL.AddRange(item.SetNoSwap());
                        }
                        return resultL.ToArray();
                    }
                    throw new ArgumentException("Unsupported Type!");
                }
                return boolEnum.Select(b => (bool) b ? (byte) 0x01 : (byte) 0x00).ToArray();
            }
            return enumerable;
        }

        private static void CreateAddressSpecification(IMessage msg, int index, OperationParameter opParam)
        {
            var isBit = opParam.Type == typeof(bool);
            var isString = opParam.Type == typeof(string);

            var offset = opParam.Offset;
            var length = Convert.ToUInt16(opParam.Args.Any() ? opParam.Args[0] : 0);
            if (isString) length += 2;
            var dbnr = Convert.ToUInt16(opParam.Args.Length > 1 ? opParam.Args[1] : 0);
            var size = isBit || opParam.Area == PlcArea.CT || opParam.Area == PlcArea.TM ? (byte)ItemDataTransportSize.Bit : (byte)ItemDataTransportSize.Byte;
            var prefix = string.Format("Item[{0}].", index);
            msg.SetAttribute(prefix + "VariableSpecification", (byte)0x12);
            const byte specLength = 0x0a;
            msg.SetAttribute(prefix + "LengthOfAddressSpecification", specLength);
            msg.SetAttribute(prefix + "SyntaxId", (byte)ItemSyntaxId.S7Any);
            msg.SetAttribute(prefix + "TransportSize", (byte)size);
            msg.SetAttribute(prefix + "ItemSpecLength", length);
            msg.SetAttribute(prefix + "DbNumber", dbnr);
            msg.SetAttribute(prefix + "Area", opParam.Area);


            offset = isBit ? offset : (offset * 8);
            var address = new byte[3];
            address[2] = (byte)(offset & 0x000000FF);
            offset = offset >> 8;
            address[1] = (byte)(offset & 0x000000FF);
            offset = offset >> 8;
            address[0] = (byte)(offset & 0x000000FF);

            msg.SetAttribute(prefix + "Address", address);
            offset += specLength + 2;
        }

    }
}
