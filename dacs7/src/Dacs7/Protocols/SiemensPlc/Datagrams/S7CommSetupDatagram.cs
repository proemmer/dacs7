// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;

namespace Dacs7.Protocols.SiemensPlc
{
    internal sealed class S7CommSetupDatagram
    {
        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 8
        };

        public S7CommSetupParameterDatagram Parameter { get; set; }







        public static S7CommSetupDatagram Build(SiemensPlcProtocolContext context, int id)
        {
            //TODO we need a parameter for the UnitId
            var result = new S7CommSetupDatagram
            {
                Parameter = new S7CommSetupParameterDatagram
                {
                    MaxAmQCalling = context.MaxAmQCalling,
                    MaxAmQCalled = context.MaxAmQCalled,
                    PduLength = context.PduSize
                }
            };
            result.Header.ProtocolDataUnitReference = (ushort)id;
            return result;
        }

        public bool Correlate(S7CommSetupDatagram o1, S7CommSetupAckDataDatagram o2)
        {
            //Test ack
            if (o1.Header.RedundancyIdentification == o2.Header.Header.RedundancyIdentification)
                return true;

            return false;
        }

        public static IMemoryOwner<byte> TranslateToMemory(S7CommSetupDatagram datagram, out int memoryLength)
        {
            var result = S7HeaderDatagram.TranslateToMemory(datagram.Header, out memoryLength);
            var take = memoryLength - datagram.Header.GetHeaderSize();
            S7CommSetupParameterDatagram.TranslateToMemory(datagram.Parameter, result.Memory.Slice(datagram.Header.GetHeaderSize(), take));
            return result;
        }

        public static S7CommSetupDatagram TranslateFromMemory(Memory<byte> data)
        {
            var result = new S7CommSetupDatagram
            {
                Header = S7HeaderDatagram.TranslateFromMemory(data)
            };
            result.Parameter = S7CommSetupParameterDatagram.TranslateFromMemory(data.Slice(result.Header.GetHeaderSize()));
            return result;
        }

    }
}
