using Dacs7.Protocols.SiemensPlc;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal sealed partial class ProtocolHandler
    {

        private Task ReceivedCommunicationSetupJob(Memory<byte> buffer)
        {
            var data = S7CommSetupDatagram.TranslateFromMemory(buffer);
            Task.Run(() => HandleCommSetupAsync(data).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        private async Task HandleCommSetupAsync(S7CommSetupDatagram data)
        {
            using (var dg = S7CommSetupAckDataDatagram
                                                    .TranslateToMemory(
                                                        S7CommSetupAckDataDatagram
                                                        .BuildFrom(_s7Context, data, data.Header.ProtocolDataUnitReference), out var memoryLength))
            {
                using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                {
                    var result = await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        var oldSemaCount = _s7Context.MaxAmQCalling;
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
