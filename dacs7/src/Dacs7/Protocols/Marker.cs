using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dacs7.Helper
{
    public class Marker
    {
        #region Properties
        public IEnumerable<byte> ByteSequence { get; protected set; }
        public int OffsetInStream { get; protected set; }
        public bool IsEndMarker { get; protected set; }
        public bool IsExclusiveMarker { get; protected set; }
        public int SequenceLength { get; protected set; }
        #endregion

        #region Constructor
        public Marker(IEnumerable<byte> aByteSequence, int aOffsetInStream, bool aEndMarker = true, bool aExclusiveMarker = false)
        {
            ByteSequence = aByteSequence;
            OffsetInStream = aOffsetInStream;
            IsEndMarker = aEndMarker;
            IsExclusiveMarker = aExclusiveMarker;
            SequenceLength = aByteSequence.Count();
        }
        #endregion

        #region Override
        public override string ToString()
        {
            return string.Format("OffsetInStream: <{0}>; ByteSequence: <{1}>; IsEndMarker: <{2}>; IsExclusiveMarker: <{3}>, SequenceLength: <{4}>",
                OffsetInStream, Encoding.ASCII.GetString(ByteSequence.ToArray()), IsEndMarker, IsExclusiveMarker, SequenceLength);
        }
        #endregion
    }
}
