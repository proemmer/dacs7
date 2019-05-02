// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;

namespace Dacs7.Protocols.SiemensPlc
{
    internal sealed class S7CommSetupAckDataDatagram
    {
        public S7AckDataDatagram Header { get; set; } = new S7AckDataDatagram();

        public S7CommSetupParameterDatagram Parameter { get; set; } = new S7CommSetupParameterDatagram();


        public static S7CommSetupAckDataDatagram BuildFrom(SiemensPlcProtocolContext context, S7CommSetupDatagram incoming)
        {
            context.MaxAmQCalling = Math.Min(incoming.Parameter.MaxAmQCalling, context.MaxAmQCalling);
            context.MaxAmQCalled = Math.Min(incoming.Parameter.MaxAmQCalled, context.MaxAmQCalled);
            context.PduSize = Math.Min(incoming.Parameter.PduLength, context.PduSize);


            //TODO we need a parameter for the UnitId
            var result = new S7CommSetupAckDataDatagram
            {
                Parameter = new S7CommSetupParameterDatagram
                {
                    MaxAmQCalling = context.MaxAmQCalling,
                    MaxAmQCalled = context.MaxAmQCalled,
                    PduLength = context.PduSize
                }
            };
            result.Header.Header.ParamLength = 8;
            return result;
        }

        public static IMemoryOwner<byte> TranslateToMemory(S7CommSetupAckDataDatagram datagram, out int memoryLength)
        {
            var result = S7AckDataDatagram.TranslateToMemory(datagram.Header, out memoryLength);
            var take = memoryLength - datagram.Header.GetParameterOffset();
            S7CommSetupParameterDatagram.TranslateToMemory(datagram.Parameter, result.Memory.Slice(datagram.Header.GetParameterOffset(), take));
            return result;
        }

        public static S7CommSetupAckDataDatagram TranslateFromMemory(Memory<byte> data)
        {
            var result = new S7CommSetupAckDataDatagram
            {
                Header = S7AckDataDatagram.TranslateFromMemory(data),
            };
            result.Parameter = S7CommSetupParameterDatagram.TranslateFromMemory(data.Slice(result.Header.GetParameterOffset()));
            return result;
        }
    }
}
