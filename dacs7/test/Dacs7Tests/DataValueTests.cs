using Dacs7.Domain;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Dacs7.Tests
{
    public class DataValueTests
    {
        [Fact()]
        public void TestBitFalseDataValue()
        {
            bool value = false;
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<bool>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestBitTrueDataValue()
        {
            bool value = true;
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<bool>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestByteDataValue()
        {
            byte value = 0x01;
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<byte>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestCharDataValue()
        {
            char value = 'x';
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<char>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestUShortDataValue()
        {
            ushort value = 5;
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<ushort>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestShortDataValue()
        {
            short value = 5;
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<short>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }


        [Fact()]
        public void TestUIntDataValue()
        {
            uint value = 5;
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<uint>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestIntDataValue()
        {
            int value = 5;
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<int>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestSingleDataValue()
        {
            float value = (float)1.5;
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<float>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }


        [Fact()]
        public void TestStringDataValue()
        {
            string value = "MyString";
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<string>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestCharArrayDataValue()
        {
            char[] value = new char[] { 'H', 'e', 'l', 'l', 'o' };
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<char[]>());
            Assert.Equal("H e l l o", testValue.GetValueAsString());
            Assert.Equal("Hello", testValue.GetValueAsString(""));
        }


        [Fact()]
        public void TestByteArrayDataValue()
        {
            byte[] value = new byte[] { 0x01, 0x02, 0x55 };
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<byte[]>());

            string result = testValue.GetValueAsString();
        }





        [Fact()]
        public void TestUShortsDataValue()
        {
            List<ushort> value = new() { 5, 10 };
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<ushort>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestShortsDataValue()
        {
            List<short> value = new() { 5, 10 };
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<short>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }


        [Fact()]
        public void TestUIntsDataValue()
        {
            List<uint> value = new() { 5, 10 };
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<uint>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestIntsDataValue()
        {
            List<int> value = new() { 5, 10 };
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<int>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }

        [Fact()]
        public void TestSinglesDataValue()
        {
            List<float> value = new() { (float)5.5, (float)10.1 };
            DataValue testValue = CreateTestValue(value);
            Assert.Equal(value, testValue.GetValue<List<float>>());
            Assert.Equal(value.ToString(), testValue.GetValueAsString());
        }



        private DataValue CreateTestValue<T>(T value)
        {
            ushort countItems = 1;
            if (value is IList l)
            {
                countItems = (ushort)l.Count;
            }
            else if (value is string s)
            {
                countItems = (ushort)s.Length;
            }


            ReadItem ri = ReadItem.Create<T>("DB1", 0, countItems);
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
