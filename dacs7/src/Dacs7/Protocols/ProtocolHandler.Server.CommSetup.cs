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
                        ushort oldSemaCount = _s7Context.MaxAmQCalling;
                        _s7Context.MaxAmQCalling = data.Parameter.MaxAmQCalling;
                        _s7Context.MaxAmQCalled = data.Parameter.MaxAmQCalled;
                        _s7Context.PduSize = data.Parameter.PduLength;
                        UpdateJobsSemaphore(oldSemaCount, _s7Context.MaxAmQCalling);

                        await UpdateConnectionState(ConnectionState.Opened).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
