using System;

namespace Dacs7
{
    /// <summary>
    /// This class is used to describe a write operation in one argument
    /// </summary>
    public class WriteOperationParameter : OperationParameter
    {

        /// <summary>
        /// This property contains the data to be written to the PLC.
        /// The type of this data are stored in the property with the name Type.
        /// </summary>
        public object Data { get; set; }


        /// <summary>
        /// Create a WriteOperationParameter instance to write data to data blocks.
        /// </summary>
        /// <typeparam name="T">Specifies the type of the value we want to write.</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">This is the value we want to write to the PLC.</param>
        /// <returns></returns>
        public static WriteOperationParameter Create<T>(int dbNumber, int offset, T value)
        {
            return new WriteOperationParameter
            {
                Area = PlcArea.DB,
                Offset = offset,
                Type = typeof(T),
                Data = value,
                Args = new[] { CalculateSizeForGenericWriteOperation<T>(PlcArea.DB, value), dbNumber }
            };
        }

        /// <summary>
        /// Create a WriteOperationParameter instance to write a bit to data blocks.
        /// </summary>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset in byte to the data you want to write.</param>
        /// <param name="bitNumber">This is the number of the bit, in the byte with the given offset, we want to write.</param>
        /// <param name="value">This is the value we want to write to the PLC.</param>
        /// <returns></returns>
        public static WriteOperationParameter CreateForBit(int dbNumber, int offset, int bitNumber, bool value)
        {
            return Create(dbNumber, offset * 8 + bitNumber, value);
        }

        /// <summary>
        /// Create a WriteOperationParameter instance to write data to any area.
        /// </summary>
        /// <typeparam name="T">Specifies the type of the value we want to write.</typeparam>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">This is the value we want to write to the PLC.</param>
        /// <returns></returns>
        public static WriteOperationParameter Create<T>(PlcArea area, int offset, T value)
        {
            if (area == PlcArea.DB)
                throw new ArgumentException("The argument area could not be of type DB.");
            return new WriteOperationParameter
            {
                Area = area,
                Offset = offset,
                Type = typeof(T),
                Data = value,
                Args = new[] { CalculateSizeForGenericWriteOperation<T>(PlcArea.DB, value) }
            };
        }

        /// <summary>
        /// Create a WriteOperationParameter instance to write a bit.
        /// This is an specialization where you do not have to calculate the bit offset by yourselves.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset in byte to the data you want to write.</param>
        /// <param name="bitNumber">This is the number of the bit, in the byte with the given offset, we want to write.</param>
        /// <param name="value">This is the value we want to write to the PLC.</param>
        /// <returns></returns>
        public static WriteOperationParameter CreateForBit(PlcArea area, int offset, int bitNumber, bool value)
        {
            return Create(area, offset * 8 + bitNumber, value);
        }

        private static int CalculateSizeForGenericWriteOperation<T>(PlcArea area, T value, int length = -1)
        {
            var size = Dacs7Client.CalculateSizeForGenericWriteOperation(area, value, length, out Type elementType);
            if (typeof(T) == typeof(string)) size -= 2;
            return size;
        }

        public override OperationParameter Cut(int size)
        {
            if (Type.IsArray && Data is Array newData)
            {
                size = Math.Min(size, newData.Length);
                var resultSize = size;
                var newLength = newData.Length - size;
                var data = new object[size];
                var restData = new object[newData.Length-size];
                for (int i = 0; i < newData.Length; i++)
                {
                    if (i < size)
                    {
                        data[i] = newData.GetValue(i);
                    }
                    else
                    {
                        restData[i - size] = newData.GetValue(i);
                    }
                }
                var args = new int[Args.Length];
                args[0] = size;
                if(args.Length > 1)
                {
                    args[1] = Args[1];
                }

                

                var result = new WriteOperationParameter
                {
                    Area = Area,
                    Offset = Offset,
                    Type = Type,
                    Data = data,
                    Args = args
                };

                Data = restData;
                Args[0] = newLength;
                Offset += size;

                return result;
            }
            return null;
        }


        public override int CalcSize(int itemSize)
        {
            itemSize += 4 + Length;  // Data header = 4
            if (Type == typeof(string))
                itemSize += 2;

            if (itemSize % 2 != 0) itemSize++;
            return itemSize;
        }
    }
}
