using System;
using Xunit;

namespace Dacs7.Tests
{
    [Collection("PlcServer collection")]
    public class ReadWriteExceptionsTests
    {

        [Fact]
        public void TestWriteItemInvalidNumberOfBitsThrowsException()
        {

            Assert.Throws<Dacs7TypeNotSupportedException>(() => WriteItem.CreateFromTag("DB3.10000,x0,2", new[] { false, false }));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => WriteItem.CreateFromTag("DB3.10000,x0", new[] { false, false }));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => WriteItem.CreateFromTag("DB3.10000,x0", new[] { false, false }));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => WriteItem.CreateFromTag("DB3.10000,x0,2", false));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => WriteItem.Create("DB1", 0, 2, false));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => WriteItem.Create("DB1", 0, new[] { false, false }));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => WriteItem.Create("DB1", 0, 2, new[] { false, false }));
        }

        [Fact]
        public void TestReadItemInvalidNumberOfBitsThrowsException()
        {

            Assert.Throws<Dacs7TypeNotSupportedException>(() => ReadItem.CreateFromTag("DB3.10000,x0,2"));

            Assert.Throws<Dacs7TypeNotSupportedException>(() => ReadItem.Create<bool[]>("DB1", 0, 2));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => ReadItem.Create<bool>("DB1", 0, 2));

            Assert.Throws<Dacs7TypeNotSupportedException>(() => ReadItem.Create<bool[]>("DB1", 0));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => ReadItem.Create<bool>("DB1", 0, 2));
        }


        [Fact]
        public void TestWriteItemInvalidStringsThrowsException()
        {

            Assert.Throws<InvalidCastException>(() => WriteItem.CreateFromTag("DB2.10046,s,20", new[] { "", "" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.CreateFromTag("DB2.10046,s,5", "      "));
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.CreateFromTag("DB2.10046,s", "123456"));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => WriteItem.Create<string[]>("DB1", 0, 2, new[] { "", "" }));

        }

        [Fact]
        public void TestReadItemInvalidStringsThrowsException()
        {
            Assert.Throws<Dacs7TypeNotSupportedException>(() => ReadItem.Create<string[]>("DB1", 0));
            Assert.Throws<Dacs7TypeNotSupportedException>(() => ReadItem.Create<string[]>("DB1", 0, 2));
        }

        [Fact]
        public void TestWriteItemInvalidDataLengthThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 2, new byte[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 1, new byte[] { 0, 0 }));

            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 2, new ushort[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 1, new ushort[] { 0, 0 }));

            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 2, new short[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 1, new short[] { 0, 0 }));

            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 2, new int[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 1, new int[] { 0, 0 }));

            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 2, new uint[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 1, new uint[] { 0, 0 }));

            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 2, new float[] { 0.0f }));
            Assert.Throws<ArgumentOutOfRangeException>(() => WriteItem.Create("DB1", 0, 1, new float[] { 0.0f, 0.0f }));

        }

    }
}

