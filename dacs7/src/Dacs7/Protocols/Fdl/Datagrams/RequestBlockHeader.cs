using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Protocols.Fdl
{
    internal class RequestBlockHeader
    {
        public UInt16[] Reserved { get; set; } = new ushort[2];

        /// <summary>
        /// Length of the request block without "user_data_1" and "user_data_2" (= 80 bytes)
        /// </summary>
        public byte Length { get; set; } = 80;

        /// <summary>
        ///  User ID, available for the FDL application
        /// </summary>
        public UInt16 User { get; set; } = 0;

        /// <summary>
        /// Type of request block used (= 2). 
        /// </summary>
        public byte RbType { get; set; } = 2;

        /// <summary>
        ///  Priority of the job. 
        /// </summary>
        public Priority Priority { get; set; } = Priority.Low;
        public byte Reserved1 { get; set; } 
        public UInt16 Reserved2 { get; set; }

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
        public UInt16 Response { get; set; } = 0x00;

        /// <summary>
        /// Number of relevant bytes in data buffer 1. 
        /// Length of data  13 to 258
        /// </summary>
        public UInt16 FillLength1 { get; set; } = 246;


        public byte Reserved3 { get; set; }

        /// <summary>
        /// Actual length of data buffer 1. 
        /// Length of BUffer
        /// </summary>
        public UInt16 SegLength1 { get; set; } = 255;

        /// <summary>
        /// Offset of data buffer 1 relative to the start of the request block. 
        /// </summary>
        public UInt16 Offset1 { get; set; } = 80;


        public UInt16 Reserved4 { get; set; }

        /// <summary>
        /// Number of relevant bytes in data buffer 2.
        /// </summary>
        public UInt16 FillLength2 { get; set; } = 0;
        public byte Reserved5 { get; set; }

        /// <summary>
        /// Actual length of data buffer 2. 
        /// </summary>
        public UInt16 SegLength2 { get; set; } = 0;

        /// <summary>
        /// Offset of data buffer 2 relative to the start of the request block.

        /// </summary>
        public UInt16 Offset2 { get; set; } = 0;

        public UInt16 Reserved6 { get; set; }
    }
}
