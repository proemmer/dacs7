using Dacs7.Helper;
using System;

namespace Dacs7.Domain
{

    internal class ReadReference
    {
        /// <summary>
        /// offset in the given data
        /// </summary>
        public int DataOffset { get; private set; }

        /// <summary>
        /// Current offset in the plc
        /// </summary>
        public int PlcOffset { get; private set; }

        /// <summary>
        /// legth of ata to write
        /// </summary>
        public ushort Length { get; private set; }


        public object Data { get; set; }

        public ReadReference(int dataOffset, int plcOffset, ushort lenght)
        {
            DataOffset = dataOffset;
            PlcOffset = plcOffset;
            Length = lenght;

        }

    }
}
