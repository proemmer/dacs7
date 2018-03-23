// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7CommSetupDatagram
    {
        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 8
        };

        public S7CommSetupParameterDatagram Parameter { get; set; }







        public static S7CommSetupDatagram Build(SiemensPlcProtocolContext context)
        {
            //TODO we need a parameter for the UnitId
            var result = new S7CommSetupDatagram
            {
                Parameter = new S7CommSetupParameterDatagram
                {
                    MaxAmQCalling = context.MaxParallelJobs,
                    MaxAmQCalled = context.MaxParallelJobs,
                    PduLength = context.PduSize
                }
            };
            return result;
        }

        public bool Correlate(S7CommSetupDatagram o1, S7CommSetupAckDataDatagram o2)
        {
            //Test ack
            if (o1.Header.RedundancyIdentification == o2.Header.Header.RedundancyIdentification)
                return true;

            return false;
        }

        public static Memory<byte> TranslateToMemory(S7CommSetupDatagram datagram)
        {
            var result = S7HeaderDatagram.TranslateToMemory(datagram.Header);
            S7CommSetupParameterDatagram.TranslateToMemory(datagram.Parameter, result.Slice(datagram.Header.GetHeaderSize()));
            return result;
        }

        public static S7CommSetupDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7CommSetupDatagram
            {
                Header = S7HeaderDatagram.TranslateFromMemory(data)
            };
            result.Parameter = S7CommSetupParameterDatagram.TranslateFromMemory(data.Slice(result.Header.GetHeaderSize()));
            return result;
        }

    }
}
