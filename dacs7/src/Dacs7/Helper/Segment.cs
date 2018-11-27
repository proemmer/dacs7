using System;
using System.Buffers;
using System.Collections.Generic;

internal static class BufferFactory
{

    private class ReadOnlyBufferSegment : ReadOnlySequenceSegment<byte>

    {

        public static ReadOnlySequence<byte> Create(IEnumerable<Memory<byte>> buffers)
        {
       
            ReadOnlyBufferSegment segment = null;
            ReadOnlyBufferSegment first = null;
            foreach (Memory<byte> buffer in buffers)

            {

                ReadOnlyBufferSegment newSegment = new ReadOnlyBufferSegment()
                {
                    Memory = buffer,
                };


                if (segment != null)
                {
                    segment.Next = newSegment;
                    newSegment.RunningIndex = segment.RunningIndex + segment.Memory.Length;
                }
                else
                { 
                    first = newSegment;
                }

                segment = newSegment;

            }



            if (first == null)
            {
                first = segment = new ReadOnlyBufferSegment();
            }

            return new ReadOnlySequence<byte>(first, 0, segment, segment.Memory.Length);

        }

    }

    public static ReadOnlySequence<byte> Create(params byte[][] buffers)
    {
        if (buffers.Length == 1)
            return new ReadOnlySequence<byte>(buffers[0]);

        List<Memory<byte>> list = new List<Memory<byte>>();
        foreach (byte[] buffer in buffers)
            list.Add(buffer);

        return Create(list.ToArray());
    }


    public static ReadOnlySequence<byte> Create(IEnumerable<Memory<byte>> buffers) 
        =>  ReadOnlyBufferSegment.Create(buffers);
}