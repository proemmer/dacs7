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

        private Task ReceivedWriteJob(Memory<byte> buffer)
        {
            if (_provider != null)
            {
                S7WriteJobDatagram data = S7WriteJobDatagram.TranslateFromMemory(buffer);
                _ = Task.Run(() => HandleWriteJobAsync(data));
            }
            return Task.CompletedTask;
        }

        private async Task HandleWriteJobAsync(S7WriteJobDatagram data)
        {
            List<WriteRequestItem> writeRequests = new();
            try
            {
                if (data.Header.GetMemorySize() > _s7Context.PduSize)
                {
                    // a plc rejects a job which does not fit into the negotiated pdu
                    _logger?.LogWarning("Write job {reference} does not fit into the pdu size of {pduSize}.", data.Header.ProtocolDataUnitReference, _s7Context.PduSize);
                    await SendErrorAckAsync(data.Header.ProtocolDataUnitReference, PduSizeErrorClass, PduSizeErrorCode).ConfigureAwait(false);
                    return;
                }

                List<S7DataItemSpecification>.Enumerator dataEnum = data.Data.GetEnumerator();
                foreach (S7AddressItemSpecificationDatagram rq in data.Items)
                {
                    dataEnum.MoveNext();
                    writeRequests.Add(new WriteRequestItem((PlcArea)rq.Area, rq.DbNumber, rq.ItemSpecLength, rq.Offset, (ItemDataTransportSize)rq.TransportSize, rq.Address, dataEnum.Current.Data));
                }

                List<WriteResultItem> results = await _provider.WriteAsync(writeRequests).ConfigureAwait(false);
                if (results == null || results.Count != writeRequests.Count)
                {
                    throw new InvalidOperationException($"The data provider returned {results?.Count ?? 0} results for {writeRequests.Count} write items.");
                }
                await SendWriteJobAck(results, data.Header.ProtocolDataUnitReference).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while handling write job {reference}.", data.Header.ProtocolDataUnitReference);
            }

            // the client always needs an answer, otherwise it runs into a timeout
            try
            {
                if (writeRequests.Count == data.Items.Count)
                {
                    await SendWriteJobAck(writeRequests.Select(rq => new WriteResultItem(rq, ItemResponseRetValue.HardwareFault)).ToList(), data.Header.ProtocolDataUnitReference).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while sending the error response for write job {reference}.", data.Header.ProtocolDataUnitReference);
            }
        }

        private async Task SendWriteJobAck(List<WriteResultItem> writeItems, ushort id)
        {
            using (System.Buffers.IMemoryOwner<byte> dgmem = S7WriteJobAckDatagram.TranslateToMemory(S7WriteJobAckDatagram.Build(_s7Context, id, writeItems), out int commemLength))
            {
                using (System.Buffers.IMemoryOwner<byte> sendData = _transport.Build(dgmem.Memory.Slice(0, commemLength), out int sendLength))
                {
                    SocketError result = await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        // ok 
                    }
                }
            }
        }

    }
}
