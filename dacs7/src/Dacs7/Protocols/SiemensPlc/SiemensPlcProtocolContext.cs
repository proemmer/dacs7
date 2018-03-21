using Microsoft.Extensions.Logging;
using System.Collections.Generic;

// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Protocols.SiemensPlc
{
    /// <summary>
    /// Context class for the protocol instance
    /// Contains all properties for the protocol
    /// </summary>
    public class SiemensPlcProtocolContext
    {

        public string Name { get; set; }
        public ushort MaxParallelJobs { get; set; } = 10;
        public ushort PduSize { get; set; } = 960;


        public SiemensPlcProtocolContext()
        {

        }

        public string Ip { get; set; } = "127.0.0.1";
        public string LocalTsap { get; set; } = "0100";
        public string RemoteTsap { get; set; } = CalcRemoteTsap(0x01, 0, 2);

        private static string CalcRemoteTsap(ushort connectionType, int rack, int slot)
        {
            var value = ((ushort)connectionType << 8) + (rack * 0x20) + slot;
            return string.Format("{0:X4}", value);
        }

    }
}
