using Dacs7.Helper;
using Dacs7.Protocols.S7;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7
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
                return true;
            });
        }

        public static Task StopPlcAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => client.StopPlc(), client.TaskCreationOptions);
        }


        public static void StartPlc(this Dacs7Client client)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreatePlcStartRequest(id);
            var policy = new S7InvocationProtocolPolicy();
            client.Logger?.LogDebug($"GetPlcTime: ProtocolDataUnitReference is {id}");
            client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0x00);
                return true;
            });
        }

        public static Task StartPlcAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => client.StartPlc(), client.TaskCreationOptions);
        }
    }
}
