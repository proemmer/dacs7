using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal sealed partial class ProtocolHandler
    {

        private Task ReceivedCommunicationSetupJob(Memory<byte> buffer)
        {
            S7CommSetupDatagram data = S7CommSetupDatagram.TranslateFromMemory(buffer);
            _ = Task.Run(() => HandleCommSetupAsync(data));
            return Task.CompletedTask;
        }

        private async Task HandleCommSetupAsync(S7CommSetupDatagram data)
        {
            try
            {
                await SendCommSetupAckAsync(data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while handling communication setup {reference}.", data.Header.ProtocolDataUnitReference);
            }
        }

        /// <summary>
        /// Error class and code a plc sends, if a job does not fit into the negotiated pdu size.
        /// </summary>
        private const byte PduSizeErrorClass = 0x85;
        private const byte PduSizeErrorCode = 0x00;

        /// <summary>
        /// Sends an ack without data, like a plc does if it rejects a job.
        /// </summary>
        private async Task SendErrorAckAsync(ushort id, byte errorClass, byte errorCode)
        {
            S7AckDataDatagram ack = new();
            ack.Header.PduType = 0x02; // Ack
            ack.Header.ProtocolDataUnitReference = id;
            ack.Error.ErrorClass = errorClass;
            ack.Error.ErrorCode = errorCode;
            using (System.Buffers.IMemoryOwner<byte> dg = S7AckDataDatagram.TranslateToMemory(ack, out int memoryLength))
            {
                using (System.Buffers.IMemoryOwner<byte> sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out int sendLength))
                {
                    await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                }
            }
        }

        private async Task SendCommSetupAckAsync(S7CommSetupDatagram data)
        {
            using (System.Buffers.IMemoryOwner<byte> dg = S7CommSetupAckDataDatagram
                                                    .TranslateToMemory(
                                                        S7CommSetupAckDataDatagram
                                                        .BuildFrom(_s7Context, data, data.Header.ProtocolDataUnitReference), out int memoryLength))
            {
                using (System.Buffers.IMemoryOwner<byte> sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out int sendLength))
                {
                    SocketError result = await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        // the context already contains the negotiated values, which were sent to the client
                        UpdateJobsSemaphore(_s7Context.MaxAmQCalling, _s7Context.MaxAmQCalling);

                        await UpdateConnectionState(ConnectionState.Opened).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
