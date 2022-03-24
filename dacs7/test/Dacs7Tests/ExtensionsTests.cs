using Dacs7.ReadWrite;
using Xunit;

namespace Dacs7.Tests
{

    public class ExtensionsTests
    {
        [Fact]
        public void GetReadItemMaxLengthForPdu120Test()
        {
            ushort pdu = 240;
            int max = pdu - 18;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetReadItemMaxLength());
        }

        [Fact]
        public void GetReadItemMaxLengthForPdu240Test()
        {
            ushort pdu = 240;
            int max = pdu - 18;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetReadItemMaxLength());
        }

        [Fact]
        public void GetReadItemMaxLengthForPdu480Test()
        {
            ushort pdu = 480;
            int max = pdu - 18;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetReadItemMaxLength());
        }


        [Fact]
        public void GetReadItemMaxLengthForclientTest()
        {
            ushort pdu = 960;
            int max = pdu - 18;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetReadItemMaxLength());
        }

        [Fact]
        public void GetReadItemMaxLengthForPdu1920Test()
        {
            ushort pdu = 1920;
            int max = pdu - 18;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetReadItemMaxLength());
        }

        [Fact]
        public void GetWriteItemMaxLengthForPdu120Test()
        {
            ushort pdu = 240;
            int max = pdu - 28;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetWriteItemMaxLength());
        }

        [Fact]
        public void GetWriteItemMaxLengthForPdu240Test()
        {
            ushort pdu = 240;
            int max = pdu - 28;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetWriteItemMaxLength());
        }

        [Fact]
        public void GetWriteItemMaxLengthForPdu480Test()
        {
            ushort pdu = 480;
            int max = pdu - 28;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetWriteItemMaxLength());
        }


        [Fact]
        public void GetWriteItemMaxLengthForclientTest()
        {
            ushort pdu = 960;
            int max = pdu - 28;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetWriteItemMaxLength());
        }


        [Fact]
        public void GetWriteItemMaxLengthForPdu1920Test()
        {
            ushort pdu = 1920;
            int max = pdu - 28;
            Dacs7Client client = new("120.0.0.1") { PduSize = pdu };
            Assert.Equal(pdu, client.PduSize);
            Assert.Equal(max, client.GetWriteItemMaxLength());
        }
    }
}
