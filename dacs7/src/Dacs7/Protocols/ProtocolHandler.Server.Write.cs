﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
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

        private Task ReceivedWriteJob(Memory<byte> buffer)
        {
            if (_provider != null)
            {
                S7WriteJobDatagram data = S7WriteJobDatagram.TranslateFromMemory(buffer);
                Task.Run(() => HandleWriteJobAsync(data).ConfigureAwait(false));
            }
            return Task.CompletedTask;
        }

        private async Task HandleWriteJobAsync(S7WriteJobDatagram data)
        {
            List<WriteRequestItem> writeRequests = new();
            List<S7DataItemSpecification>.Enumerator dataEnum = data.Data.GetEnumerator();
            foreach (S7AddressItemSpecificationDatagram rq in data.Items)
            {
                dataEnum.MoveNext();
                writeRequests.Add(new WriteRequestItem((PlcArea)rq.Area, rq.DbNumber, rq.ItemSpecLength, rq.Offset, (ItemDataTransportSize)rq.TransportSize, rq.Address, dataEnum.Current.Data));
            }

            List<WriteResultItem> results = await _provider.WriteAsync(writeRequests).ConfigureAwait(false);
            await SendWriteJobAck(results, data.Header.ProtocolDataUnitReference).ConfigureAwait(false);
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
