using Dacs7.Domain;
using Dacs7.Helper;
using Dacs7.Protocols.S7;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.Control
{
    public static class PlcControlExtensions
    {
        /// <summary>
        /// Read the plc time
        /// </summary>
        /// <returns>The current <see cref="DateTime"/> from the PLC.</returns>
        public static DateTime GetPlcTime(this Dacs7Client client)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateReadClockRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            client.Logger?.LogDebug($"GetPlcTime: ProtocolDataUnitReference is {id}");
            return (DateTime)client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0xff);
                var sslData = cbh.ResponseMessage.GetAttribute("SSLData", new byte[0]);
                if (sslData.Any())
                    return sslData.ConvertToDateTime(2);
                throw new InvalidDataException("SSL Data are empty!");
            });
        }

        /// <summary>
        /// Read the plc time async
        /// </summary>
        /// <returns>The current <see cref="DateTime"/> from the PLC as a <see cref="Task"/></returns>
        public static Task<DateTime> GetPlcTimeAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => client.GetPlcTime(), client.TaskCreationOptions);
        }



        /// <summary>
        /// Set the plc time
        /// </summary>
        public static void SetPlcTime(this Dacs7Client client, DateTime dateTime)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var dt = new List<byte> { 0x00 };
            dt.AddRange(dateTime.ConvertFromDateTime());
            var reqMsg = S7MessageCreator.CreateWriteClockRequest(id, dt.ToArray());
            var policy = new S7UserDataProtocolPolicy();
            client.Logger?.LogDebug($"GetPlcTime: ProtocolDataUnitReference is {id}");
            client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0x0a);
                cbh.ResponseMessage.EnsureValidErrorClass();
                return true;
            });
        }

        /// <summary>
        /// Set the plc time async
        /// </summary>
        /// <returns>The current <see cref="DateTime"/> from the PLC as a <see cref="Task"/></returns>
        public static Task SetPlcTimeAsync(this Dacs7Client client, DateTime dateTime)
        {
            return Task.Factory.StartNew(() => client.SetPlcTime(dateTime), client.TaskCreationOptions);
        }

        public static PlcStateInfo GetPlcState(this Dacs7Client client)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateSllRequest(id, 0x0424, 0x0000); // plc state
            var policy = new S7UserDataProtocolPolicy();
            client.Logger?.LogDebug($"GetPlcState: ProtocolDataUnitReference is {id}");
            return (PlcStateInfo)client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                var returnCode = cbh.ResponseMessage.GetAttribute("ReturnCode", (byte)0);
                if (returnCode == 0xff)
                {
                    var sslData = cbh.ResponseMessage.GetAttribute("SSLData", new byte[0]);
                    if (sslData.Any())
                    {
                        var res = new PlcStateInfo
                        {
                            State = (PlcStates)sslData[11],
                            PreviousState = (PlcStates)sslData[16],
                            Timestamp = new DateTime(2000 + sslData[20].GetBcdByte(), 
                                                    sslData[21].GetBcdByte(), 
                                                    sslData[22].GetBcdByte(), 
                                                    sslData[23].GetBcdByte(), 
                                                    sslData[24].GetBcdByte(), 
                                                    sslData[25].GetBcdByte())
                        };
                        return res;
                    }
                    throw new InvalidDataException("SSL Data are empty!");
                }
                throw new Dacs7ReturnCodeException(returnCode);
            });
        }


        /// <summary>
        /// Stopps the plc.
        /// </summary>
        /// <param name="client"></param>
        public static void StopPlc(this Dacs7Client client)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreatePlcStopRequest(id);
            var policy = new S7InvocationProtocolPolicy();
            client.Logger?.LogDebug($"StopPlc: ProtocolDataUnitReference is {id}");
            client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0x00);
                cbh.ResponseMessage.EnsureValidErrorClass();
                return true;
            });
        }

        /// <summary>
        /// Stopps the plc.
        /// </summary>
        /// <param name="client"></param>
        public static Task StopPlcAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => client.StopPlc(), client.TaskCreationOptions);
        }

        /// <summary>
        /// Starts the plc if the operation switch is in the correct position.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="coldStart"></param>
        public static void StartPlc(this Dacs7Client client, bool coldStart = false)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateInvocationRequest(id, "P_PROGRAM", coldStart ? "C " :"");
            var policy = new S7InvocationProtocolPolicy();
            client.Logger?.LogDebug($"StartPlc: ProtocolDataUnitReference is {id}");
            client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0x00);
                cbh.ResponseMessage.EnsureValidErrorClass();
                return true;
            });
        }

        /// <summary>
        /// Starts the plc if the operation switch is in the correct position.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="coldStart"></param>
        public static Task StartPlcAsync(this Dacs7Client client, bool coldStart = false)
        {
            return Task.Factory.StartNew(() => client.StartPlc(coldStart), client.TaskCreationOptions);
        }


        /// <summary>
        /// Copy data from ram to rom
        /// </summary>
        /// <param name="client"></param>
        public static void CopyRamToRom(this Dacs7Client client)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateInvocationRequest(id, "_MODU", "PE" );
            var policy = new S7InvocationProtocolPolicy();
            client.Logger?.LogDebug($"CopyRamToRom: ProtocolDataUnitReference is {id}");
            client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0x00);
                cbh.ResponseMessage.EnsureValidErrorClass();
                return true;
            });
        }

        /// <summary>
        /// Copy data from ram to rom async
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static Task CopyRamToRomAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => client.CopyRamToRom(), client.TaskCreationOptions);
        }

        /// <summary>
        /// Compress the momory of the plc
        /// </summary>
        /// <param name="client"></param>
        public static void CompressMemory(this Dacs7Client client)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateInvocationRequest(id, "_GARB", "");
            var policy = new S7InvocationProtocolPolicy();
            client.Logger?.LogDebug($"CompressMemory: ProtocolDataUnitReference is {id}");
            client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0x00);
                cbh.ResponseMessage.EnsureValidErrorClass();
                return true;
            });
        }

        /// <summary>
        /// Compress the momory of the plc
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static Task CompressMemoryAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => client.CompressMemory(), client.TaskCreationOptions);
        }
    }
}
