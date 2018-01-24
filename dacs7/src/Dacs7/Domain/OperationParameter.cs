using System;
using System.Linq;

namespace Dacs7
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


        public virtual int CalcSize(int itemSize) => itemSize;


        public abstract OperationParameter Cut(int size);
    }
}
