// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Protocols.Fdl
{
    internal sealed class RequestBlockHeader
    {
        public ushort[] Reserved { get; set; } = new ushort[2];

        /// <summary>
        /// Length of the request block without "user_data_1" and "user_data_2" (= 80 bytes)
        /// </summary>
        public byte Length { get; set; } = 80;

        /// <summary>
        ///  User ID, available for the FDL application
        /// </summary>
        public ushort User { get; set; } = 0;

        /// <summary>
        /// Type of request block used (= 2). 
        /// </summary>
        public byte RbType { get; set; } = 2;

        /// <summary>
        ///  Priority of the job. 
        /// </summary>
        public Priority Priority { get; set; } = Priority.Low;
        public byte Reserved1 { get; set; }
        public ushort Reserved2 { get; set; }

        /// <summary>
        /// Communication layer selection (FDL = 22h). 
        /// </summary>
        public byte Subsystem { get; set; } = 0x22;

        /// <summary>
        /// Request, confirm, indication (same as the parameter "opcode" in the application block). 
        /// </summary>
        public ComClass OpCode { get; set; } = ComClass.Request;

        /// <summary>
        /// Return parameter (same as the parameter "l_status" in the application block). 
        /// </summary>
        public ushort Response { get; set; } = 0x00;

        /// <summary>
        /// Number of relevant bytes in data buffer 1. 
        /// Length of data  13 to 258
        /// </summary>
        public ushort FillLength1 { get; set; } = 246;


        public byte Reserved3 { get; set; }

        /// <summary>
        /// Actual length of data buffer 1. 
        /// Length of BUffer
        /// </summary>
        public ushort SegLength1 { get; set; } = 255;

        /// <summary>
        /// Offset of data buffer 1 relative to the start of the request block. 
        /// </summary>
        public ushort Offset1 { get; set; } = 80;


        public ushort Reserved4 { get; set; }

        /// <summary>
        /// Number of relevant bytes in data buffer 2.
        /// </summary>
        public ushort FillLength2 { get; set; } = 0;
        public byte Reserved5 { get; set; }

        /// <summary>
        /// Actual length of data buffer 2. 
        /// </summary>
        public ushort SegLength2 { get; set; } = 0;

        /// <summary>
        /// Offset of data buffer 2 relative to the start of the request block.

        /// </summary>
        public ushort Offset2 { get; set; } = 0;

        public ushort Reserved6 { get; set; }
    }
}
