using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.Domain
{
    /// <summary>
    /// This class is the abstract base class of the read and write operations
    /// </summary>
    public abstract class OperationParameter
    {
        /// <summary>
        /// This property specifies the area of the address <see cref="PlcArea"/>
        /// </summary>
        public PlcArea Area { get; set; }

        /// <summary>
        /// This property specifies the offset from the begin of the area.
        /// This is normally in number of bytes, but if the type is a boolean, the
        /// number of bits will be used, to also address a bit in a byte. ([OffsetInBytes]*8+[BitNumber])
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// This property describes the data type to read or write
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Arguments depending on the area. First argument is the data length in byte. If area is DB, second parameter is the db number 
        /// </summary>
        public int[] Args { get; set; }

        /// <summary>
        /// Readable length property
        /// </summary>
        public int Length
        {
            get
            {
                return Args != null && Args.Any() ? Args.First() : 0;
            }
        }
    }


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
    }

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
            Type elementType;
            var size = Dacs7Client.CalculateSizeForGenericWriteOperation<T>(area, value, length, out elementType);
            if (typeof(T) == typeof(string)) size -= 2;
            return size;
        }
    }
}
