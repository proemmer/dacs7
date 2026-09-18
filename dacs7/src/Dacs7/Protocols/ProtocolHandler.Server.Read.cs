// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal sealed partial class ProtocolHandler
    {

        private Task ReceivedReadJob(Memory<byte> buffer)
        {
            if (_provider != null)
            {
                S7ReadJobDatagram data = S7ReadJobDatagram.TranslateFromMemory(buffer);
                _ = Task.Run(() => HandleReadJobAsync(data)); // here we do not have to wayt because the receive buffer is fully converted and is not needed anymore
            }
            return Task.CompletedTask;
        }

        private async Task HandleReadJobAsync(S7ReadJobDatagram data)
        {
            List<ReadRequestItem> readRequests = null;
            try
            {
                readRequests = data.Items.Select(rq => new ReadRequestItem((PlcArea)rq.Area, rq.DbNumber, rq.ItemSpecLength, rq.Offset, (ItemDataTransportSize)rq.TransportSize, rq.Address)).ToList();
                List<ReadResultItem> results = await _provider.ReadAsync(readRequests).ConfigureAwait(false);
                if (results == null || results.Count != readRequests.Count)
                {
                    throw new InvalidOperationException($"The data provider returned {results?.Count ?? 0} results for {readRequests.Count} read items.");
                }
                await SendReadJobAck(results, data.Header.ProtocolDataUnitReference).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while handling read job {reference}.", data.Header.ProtocolDataUnitReference);
            }

            // the client always needs an answer, otherwise it runs into a timeout
            try
            {
                if (readRequests != null)
                {
                    await SendReadJobAck(readRequests.Select(rq => new ReadResultItem(rq, ItemResponseRetValue.HardwareFault)).ToList(), data.Header.ProtocolDataUnitReference).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while sending the error response for read job {reference}.", data.Header.ProtocolDataUnitReference);
            }
        }

        private async Task SendReadJobAck(List<ReadResultItem> readItems, ushort id)
        {
            using (System.Buffers.IMemoryOwner<byte> dgmem = S7ReadJobAckDatagram.TranslateToMemory(S7ReadJobAckDatagram.Build(_s7Context, id, readItems), out int commemLength))
            {
                using (System.Buffers.IMemoryOwner<byte> sendData = _transport.Build(dgmem.Memory.Slice(0, commemLength), out int sendLength))
                {
                    SocketError result = await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        // OK
                    }
                }
            }
        }

    }
}
