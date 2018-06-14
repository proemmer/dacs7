using Dacs7;
using Dacs7.Domain;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Dacs7Tests
{
    public class DataValueTests
    {
        [Fact()]
        public void TestBitFalseDataValue()
        {
            var value = false;
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<bool>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestBitTrueDataValue()
        {
            var value = true;
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<bool>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestByteDataValue()
        {
            var value = (byte)0x01;
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<byte>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestCharDataValue()
        {
            var value = 'x';
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<char>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestUShortDataValue()
        {
            var value = (ushort)5;
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<ushort>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestShortDataValue()
        {
            var value = (short)5;
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<short>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }


        [Fact()]
        public void TestUIntDataValue()
        {
            var value = (uint)5;
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<uint>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestIntDataValue()
        {
            var value = (int)5;
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<int>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestSingleDataValue()
        {
            var value = (Single)1.5;
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<Single>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }


        [Fact()]
        public void TestStringDataValue()
        {
            var value = "MyString";
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<string>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestCharArrayDataValue()
        {
            var value = new char[] { 'H', 'e', 'l', 'l', 'o' };
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<char[]>());
            Assert.Equal("H e l l o", testValue.GetValueAsString());
            Assert.Equal("Hello", testValue.GetValueAsString(""));
        }


        [Fact()]
        public void TestByteArrayDataValue()
        {
            var value = new byte[] { 0x01, 0x02, 0x55 };
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<byte[]>());

            var result = testValue.GetValueAsString();
        }





        [Fact()]
        public void TestUShortsDataValue()
        {
            var value = new List<ushort> { 5, 10 };
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<ushort>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestShortsDataValue()
        {
            var value = new List<short> { 5, 10 };
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<short>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }


        [Fact()]
        public void TestUIntsDataValue()
        {
            var value = new List<uint> { 5, 10 };
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<uint>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestIntsDataValue()
        {
            var value = new List<int> { 5, 10 };
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<int>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestSinglesDataValue()
        {
            var value = new List<Single> { (Single)5.5, (Single)10.1 };
            var testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<Single>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }



        private DataValue CreateTestValue<T>(T value)
        {
            ushort countItems = 1;
            if(value is IList l)
            {
                countItems = (ushort)l.Count;
            }
            else if(value is string s)
            {
                countItems = (ushort)s.Length;
            }


            var ri = ReadItem.Create<T>("DB1", 0, countItems);
            Memory<byte> itemData = ri.ConvertDataToMemory(value);

            Memory<byte> buffer = new byte[4 + itemData.Length];
            buffer.Span[0] = 255;
            buffer.Span[1] = 3;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Span.Slice(2, 2), (ushort)itemData.Length);
            itemData.CopyTo(buffer.Slice(4));
            return new DataValue(ri, S7DataItemSpecification.TranslateFromMemory(buffer));
        }
    }
}
