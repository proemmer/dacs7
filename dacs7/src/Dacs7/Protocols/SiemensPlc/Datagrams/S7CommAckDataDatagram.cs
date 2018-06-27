// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{
    internal class S7CommSetupAckDataDatagram
    {
        public S7AckDataDatagram Header { get; set; } = new S7AckDataDatagram();

        public S7CommSetupParameterDatagram Parameter { get; set; } = new S7CommSetupParameterDatagram();


        public static S7CommSetupAckDataDatagram BuildFrom(SiemensPlcProtocolContext context, S7CommSetupDatagram incoming)
        {
            context.MaxParallelJobs = Math.Min(incoming.Parameter.MaxAmQCalling, context.MaxParallelJobs);
            context.MaxParallelJobs = Math.Min(incoming.Parameter.MaxAmQCalled, context.MaxParallelJobs);
            context.PduSize = Math.Min(incoming.Parameter.PduLength, context.PduSize);


            //TODO we need a parameter for the UnitId
            var result = new S7CommSetupAckDataDatagram
            {
                Parameter = new S7CommSetupParameterDatagram
                {
                    MaxAmQCalling =  context.MaxParallelJobs,
                    MaxAmQCalled = context.MaxParallelJobs,
                    PduLength = context.PduSize
                }
            };
            result.Header.Header.ParamLength = 8;
            return result;
        }

        public static Memory<byte> TranslateToMemory(S7CommSetupAckDataDatagram datagram)
        {
            var result = S7AckDataDatagram.TranslateToMemory(datagram.Header);
            S7CommSetupParameterDatagram.TranslateToMemory(datagram.Parameter, result.Slice(datagram.Header.GetParameterOffset()));
            return result;
        }

        public static S7CommSetupAckDataDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7CommSetupAckDataDatagram
            {
                Header = S7AckDataDatagram.TranslateFromMemory(data),
            };
            result.Parameter = S7CommSetupParameterDatagram.TranslateFromMemory(data.Slice(result.Header.GetParameterOffset()));
            return result;
        }
    }
}
