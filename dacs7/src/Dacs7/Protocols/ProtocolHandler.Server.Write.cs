// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal sealed partial class ProtocolHandler
    {

        private async Task ReceivedWriteJob(Memory<byte> buffer)
        {
            
            if (_provider != null)
            {
                var data = S7WriteJobDatagram.TranslateFromMemory(buffer);
                var writeRequests = new List<WriteRequestItem>();
                var dataEnum = data.Data.GetEnumerator();
                foreach (var rq in data.Items)
                {
                    dataEnum.MoveNext();
                    writeRequests.Add(new WriteRequestItem((PlcArea)rq.Area, rq.DbNumber, rq.ItemSpecLength, rq.Offset, (ItemDataTransportSize)rq.TransportSize, rq.Address, dataEnum.Current.Data));
                }

                var results = await _provider.WriteAsync(writeRequests).ConfigureAwait(false);
                await SendWriteJobAck(results).ConfigureAwait(false);
            }
        }

        private async Task SendWriteJobAck(List<WriteResultItem> writeItems)
        {
            using (var dgmem = S7WriteJobAckDatagram.TranslateToMemory(S7WriteJobAckDatagram.Build(_s7Context, GetNextReferenceId(), writeItems), out var commemLength))
            {
                using (var sendData = _transport.Build(dgmem.Memory.Slice(0, commemLength), out var sendLength))
                {
                    var result = await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        // ok 
                    }
                }
            }
        }

    }
}
