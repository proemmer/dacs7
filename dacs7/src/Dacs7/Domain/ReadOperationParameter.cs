using Dacs7.Domain;
using System;

namespace Dacs7
{
    /// <summary>
    /// This class is used to describe a read operation in one argument
    /// </summary>
    public class ReadOperationParameter : OperationParameter
    {
        /// <summary>
        /// Create a ReadOperationParameter instance to read data from data blocks.
        /// </summary>
        /// <typeparam name="T">Specifies the type of the value we want to read.</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="numberOfItems">Number of items of the T to read. This could be the string length for a string, or the number of bytes/int for an array and so on. 
        /// The default value is always 1.
        /// Reading array of BOOLs is not supported in this case at the moment!</param>
        /// <returns></returns>
        public static ReadOperationParameter Create<T>(int dbNumber, int offset, int numberOfItems = -1)
        {
            var t = typeof(T);
            CalculateElementLength<T>(ref offset, ref numberOfItems, t, PlcArea.DB);

            return new ReadOperationParameter
            {
                Area = PlcArea.DB,
                Offset = offset,
                Type = t,
                Args = new[] { numberOfItems, dbNumber }
            };
        }

        /// <summary>
        /// Create a ReadOperationParameter instance to read a bit from a data block.
        /// </summary>
        /// <typeparam name="T">Specifies the type of the value we want to read.</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset in byte to the data you want to read.</param>
        /// <param name="bitNumber">This is the number of the bit, in the byte with the given offset, we want to read.</param>
        /// <param name="numberOfItems">Number of items of the T to read. This could be the string length for a string, or the number of bytes/int for an array and so on. 
        /// The default value is always 1.
        /// Reading array of BOOLs is not supported in this case at the moment!</param>
        /// <returns></returns>
        public static ReadOperationParameter CreateForBit(int dbNumber, int offset, int bitNumber, int numberOfItems = -1)
        {
            return Create<bool>(dbNumber, offset * 8 + bitNumber, numberOfItems);
        }


        /// <summary>
        /// Create a ReadOperationParameter instance by the given arguments
        /// </summary>
        /// <typeparam name="T">Specifies the type of the value we want to read.</typeparam>
        /// <param name="area">The target <see cref="PlcArea"></see> from which we want to read.</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="numberOfItems">Number of items of the T to read. This could be the string length for a string, or the number of bytes/int for an array and so on. 
        /// The default value is always 1.
        /// Reading array of BOOLs is not supported in this case at the moment!</param>
        /// <returns></returns>
        public static ReadOperationParameter Create<T>(PlcArea area, int offset, int numberOfItems = -1)
        {
            if (area == PlcArea.DB)
                throw new ArgumentException("The argument area could not be DB.");
            var t = typeof(T);
            CalculateElementLength<T>(ref offset, ref numberOfItems, t, area);

            return new ReadOperationParameter
            {
                Area = area,
                Offset = offset,
                Type = t,
                Args = new[] { numberOfItems }
            };
        }

        /// <summary>
        /// Create a ReadOperationParameter instance to read a bit.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> from which we want to read.</param>
        /// <param name="offset">This is the offset in byte to the data you want to read.</param>
        /// <param name="bitNumber">This is the number of the bit, in the byte with the given offset, we want to read.</param>
        /// <param name="numberOfItems">Number of items of the T to read. This could be the string length for a string, or the number of bytes/int for an array and so on. 
        /// The default value is always 1.
        /// Reading array of BOOLs is not supported in this case at the moment!</param>
        /// <returns></returns>
        public static ReadOperationParameter CreateForBit(PlcArea area, int offset, int bitNumber, int numberOfItems = -1)
        {
            return Create<bool>(area, offset * 8 + bitNumber, numberOfItems);
        }

        private static void CalculateElementLength<T>(ref int offset, ref int numberOfItems, Type t, PlcArea area)
        {
            var isBool = t == typeof(bool);
            var isString = t == typeof(string);
            var elementLength = isString ? numberOfItems : TransportSizeHelper.DataTypeToSizeByte(typeof(T), area);
            if (numberOfItems >= 0)
            {
                if (isBool)
                    offset /= 8;
                else if (isString)
                    numberOfItems = elementLength;
                else
                    numberOfItems = numberOfItems * elementLength;
            }
            else
                numberOfItems = 1;
        }

        public override OperationParameter Cut(int size)
        {
            throw new NotImplementedException();
        }
    }
}
