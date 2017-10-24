using Dacs7.Domain;
using Dacs7.Helper;
using Dacs7.Protocols.S7;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.Metadata
{
    public static class PlcMetadataExtensions
    {


        /// <summary>
        /// Read the number of blocks in the PLC per type
        /// </summary>
        /// <returns><see cref="IPlcBlocksCount"/> where you have access to the count of all the block types.</returns>
        public static IPlcBlocksCount GetBlocksCount(this Dacs7Client client)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateBlocksCountRequest(id);
            var policy = new S7UserDataProtocolPolicy();

            client.Logger?.LogDebug($"GetBlocksCount: ProtocolDataUnitReference is {id}");
            return client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
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
            }) as IPlcBlocksCount;
        }

        /// <summary>
        /// Read the number of blocks in the PLC per type asynchronous. This means the call is wrapped in a Task.
        /// </summary>
        /// <returns><see cref="IPlcBlocksCount"/> where you have access to the count of all the block types.</returns>
        public static Task<IPlcBlocksCount> GetBlocksCountAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => client.GetBlocksCount(), client.TaskCreationOptions);
        }

        /// <summary>
        /// Get all blocks of the specified type.
        /// </summary>
        /// <param name="type">Block type to read. <see cref="PlcBlockType"/></param>
        /// <returns>Return a list off all blocks <see cref="IPlcBlock"/> of this type</returns>
        public static  IEnumerable<IPlcBlocks> GetBlocksOfType(this Dacs7Client client, PlcBlockType type)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var policy = new S7UserDataProtocolPolicy();
            var blocks = new List<IPlcBlocks>();
            var lastUnit = false;
            var sequenceNumber = (byte)0x00;
            client.Logger?.LogDebug($"GetBlocksOfType: ProtocolDataUnitReference is {id}");

            do
            {
                var reqMsg = S7MessageCreator.CreateBlocksOfTypeRequest(id, type, sequenceNumber);

                if (client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
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
        public static Task<IEnumerable<IPlcBlocks>> GetBlocksOfTypeAsync(this Dacs7Client client, PlcBlockType type)
        {
            return Task.Factory.StartNew(() => client.GetBlocksOfType(type), client.TaskCreationOptions);
        }

        /// <summary>
        /// Read the meta data of a block from the PLC.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB   <see cref="PlcBlockType"/></param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns><see cref="IPlcBlockInfo"/> where you have access tho the detailed meta data of the block.</returns>
        public static IPlcBlockInfo ReadBlockInfo(this Dacs7Client client, PlcBlockType blockType, int blocknumber)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateBlockInfoRequest(id, blockType, blocknumber);
            var policy = new S7UserDataProtocolPolicy();
            client.Logger?.LogDebug($"ReadBlockInfo: ProtocolDataUnitReference is {id}");
            return client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
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
            }) as IPlcBlockInfo;
        }


        /// <summary>
        /// Read the meta data of a block asynchronous from the PLC.This means the call is wrapped in a Task.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB  <see cref="PlcBlockType"/></param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns>a <see cref="Task"/> of <see cref="IPlcBlockInfo"/> where you have access tho the detailed meta data of the block.</returns>
        public static Task<IPlcBlockInfo> ReadBlockInfoAsync(this Dacs7Client client, PlcBlockType blockType, int blocknumber)
        {
            return Task.Factory.StartNew(() => client.ReadBlockInfo(blockType, blocknumber), client.TaskCreationOptions);
        }






        ///// <summary>
        ///// Read the full data of a block from the PLC.
        ///// </summary>
        ///// <param name="blockType">Specify the block type to read. e.g. DB  <see cref="PlcBlockType"/></param>
        ///// <param name="blocknumber">Specify the Number of the block</param>
        ///// <returns>returns the see  <see cref="T:byte[]"/> of the block.</returns>
        //public static byte[] UploadPlcBlock(this Dacs7Client client, PlcBlockType blockType, int blocknumber)
        //{
        //    if (!client.IsConnected)
        //        throw new Dacs7NotConnectedException();
        //    var id = client.GetNextReferenceId();
        //    var policy = new S7JobUploadProtocolPolicy();
        //    client.Logger?.LogDebug($"ReadBlockInfo: ProtocolDataUnitReference is {id}");

        //    //Start Upload
        //    var reqMsg = S7MessageCreator.CreateStartUploadRequest(id, blockType, blocknumber);
        //    uint controlId = 0;
        //    client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
        //    {
        //        var function = cbh.ResponseMessage.GetAttribute("Function", (byte)0);
        //        if (function == 0x1d)
        //        {
        //            //all write operations are successfully
        //            var paramData = cbh.ResponseMessage.GetAttribute("ParameterData", new byte[0]);
        //            if (paramData.Length >= 7)
        //                controlId = paramData.GetSwap<uint>(3);
        //            return;
        //        }

        //    });

        //    //Upload packages
        //    reqMsg = S7MessageCreator.CreateUploadRequest(id, blockType, blocknumber, controlId);
        //    var data = new List<byte>();
        //    var hasNext = false;
        //    do
        //    {
        //        hasNext = false;
        //        client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
        //        {
        //            var function = cbh.ResponseMessage.GetAttribute("Function", (byte)0);
        //            if (function == 0x1e)
        //            {
        //                //all write operations are successfully
        //                var paramdata = cbh.ResponseMessage.GetAttribute("ParameterData", new byte[0]);
        //                if (paramdata.Length == 1)
        //                    hasNext = paramdata[0] == 0x01;
        //                var currentdata = cbh.ResponseMessage.GetAttribute("Data", new byte[0]).Skip(3);
        //                data.AddRange(currentdata);
        //                return;
        //            }
        //        });
        //    } while (hasNext);


        //    reqMsg = S7MessageCreator.CreateEndUploadRequest(id, blockType, blocknumber, controlId);
        //    client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
        //    {
        //        var function = cbh.ResponseMessage.GetAttribute("Function", (byte)0);
        //        if (function == 0x1f)
        //        {
        //            //all write operations are successfully
        //            return;
        //        }
        //    });

        //    return data.ToArray();
        //}


    }
}
