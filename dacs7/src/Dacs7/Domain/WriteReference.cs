using Dacs7.Helper;
using System;

namespace Dacs7.Domain
{

    internal class WriteReference
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

        /// <summary>
        /// Data to write
        /// </summary>
        public object Data { get; set; }

        public WriteReference(int dataOffset, int writeOffset, ushort lenght, object data, bool extract = true)
        {
            DataOffset = dataOffset;
            PlcOffset = writeOffset;
            Length = lenght;

            if (extract)
            {
                Data = ExtractData(data, dataOffset, lenght);
            }
            else
            {
                Data = data;
            }

        }

        private static object ExtractData(object data, int offset = 0, int length = Int32.MaxValue)
        {
            var enumerable = data as byte[];
            if (enumerable == null)
            {
                var boolEnum = data as bool[];
                if (boolEnum == null)
                {
                    if (data is bool)
                        return (bool)data;
                    if (data is byte || data is char)
                        return (byte)data;
                    return null;
                }
                return boolEnum.SubArray(offset, length);
            }
            return enumerable.SubArray(offset, length);
        }
    }
}
