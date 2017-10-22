using Dacs7.Helper;
using Dacs7.Protocols.S7;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7
{
    public static class PlcControlExtensions
    {
        /// <summary>
        /// Read the number of blocks in the PLC per type
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
        /// Read the number of blocks in the PLC per type
        /// </summary>
        /// <returns>The current <see cref="DateTime"/> from the PLC as a <see cref="Task"/></returns>
        public static Task<DateTime> GetPlcTimeAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => client.GetPlcTime(), client.TaskCreationOptions);
        }



        /// <summary>
        /// Read the number of blocks in the PLC per type
        /// </summary>
        /// <returns>The current <see cref="DateTime"/> from the PLC.</returns>
        public static void SetPlcTime(this Dacs7Client client, DateTime dateTime)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();
            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateReadClockRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            client.Logger?.LogDebug($"GetPlcTime: ProtocolDataUnitReference is {id}");
            client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0xff);
                var sslData = cbh.ResponseMessage.GetAttribute("SSLData", new byte[0]);
                if (sslData.Any())
                    return sslData.ConvertToDateTime(2);
                throw new InvalidDataException("SSL Data are empty!");
            });
        }
    }
}
