using Dacs7.Domain;
using System;
using Xunit;

namespace Dacs7.Tests
{

    public class TagParserTests
    {
        [Theory()]
        [InlineData("DB1403.88,C,8    ", PlcArea.DB, 88, 8, typeof(char[]), typeof(char))]
        [InlineData("DB1.10000,x0,1", PlcArea.DB, 80000, 1, typeof(bool))]
        [InlineData("DB1.10000,x0", PlcArea.DB, 80000, 1, typeof(bool))]
        [InlineData("DB1.10000,b,1", PlcArea.DB, 10000, 1, typeof(byte))]
        [InlineData("DB1.10000,b", PlcArea.DB, 10000, 1, typeof(byte))]
        [InlineData("DB1.10000,b,10", PlcArea.DB, 10000, 10, typeof(byte[]), typeof(byte))]
        [InlineData("DB1.10000,c,1", PlcArea.DB, 10000, 1, typeof(char))]
        [InlineData("DB1.10000,c", PlcArea.DB, 10000, 1, typeof(char))]
        [InlineData("DB1.10000,c,10", PlcArea.DB, 10000, 10, typeof(char[]), typeof(char))]
        [InlineData("DB1.10000,w,1", PlcArea.DB, 10000, 1, typeof(ushort))]
        [InlineData("DB1.10000,w", PlcArea.DB, 10000, 1, typeof(ushort))]
        [InlineData("DB1.10000,w,10", PlcArea.DB, 10000, 10, typeof(ushort[]), typeof(ushort))]
        [InlineData("DB1.10000,i,1", PlcArea.DB, 10000, 1, typeof(short))]
        [InlineData("DB1.10000,i", PlcArea.DB, 10000, 1, typeof(short))]
        [InlineData("DB1.10000,i,10", PlcArea.DB, 10000, 10, typeof(short[]), typeof(short))]
        [InlineData("DB1.10000,dw,1", PlcArea.DB, 10000, 1, typeof(uint))]
        [InlineData("DB1.10000,dw", PlcArea.DB, 10000, 1, typeof(uint))]
        [InlineData("DB1.10000,dw,10", PlcArea.DB, 10000, 10, typeof(uint[]), typeof(uint))]
        [InlineData("DB1.10000,di,1", PlcArea.DB, 10000, 1, typeof(int))]
        [InlineData("DB1.10000,di", PlcArea.DB, 10000, 1, typeof(int))]
        [InlineData("DB1.10000,di,10", PlcArea.DB, 10000, 10, typeof(int[]), typeof(int))]
        [InlineData("I.10000,x0,1", PlcArea.IB, 80000, 1, typeof(bool))]
        [InlineData("I.10000,x0", PlcArea.IB, 80000, 1, typeof(bool))]
        [InlineData("I.10000,b,1", PlcArea.IB, 10000, 1, typeof(byte))]
        [InlineData("I.10000,b", PlcArea.IB, 10000, 1, typeof(byte))]
        [InlineData("I.10000,b,10", PlcArea.IB, 10000, 10, typeof(byte[]), typeof(byte))]
        [InlineData("I.10000,c,1", PlcArea.IB, 10000, 1, typeof(char))]
        [InlineData("I.10000,c", PlcArea.IB, 10000, 1, typeof(char))]
        [InlineData("I.10000,c,10", PlcArea.IB, 10000, 10, typeof(char[]), typeof(char))]
        [InlineData("I.10000,w,1", PlcArea.IB, 10000, 1, typeof(ushort))]
        [InlineData("I.10000,w", PlcArea.IB, 10000, 1, typeof(ushort))]
        [InlineData("I.10000,w,10", PlcArea.IB, 10000, 10, typeof(ushort[]), typeof(ushort))]
        [InlineData("I.10000,i,1", PlcArea.IB, 10000, 1, typeof(short))]
        [InlineData("I.10000,i", PlcArea.IB, 10000, 1, typeof(short))]
        [InlineData("I.10000,i,10", PlcArea.IB, 10000, 10, typeof(short[]), typeof(short))]
        [InlineData("I.10000,dw,1", PlcArea.IB, 10000, 1, typeof(uint))]
        [InlineData("I.10000,dw", PlcArea.IB, 10000, 1, typeof(uint))]
        [InlineData("I.10000,dw,10", PlcArea.IB, 10000, 10, typeof(uint[]), typeof(uint))]
        [InlineData("I.10000,di,1", PlcArea.IB, 10000, 1, typeof(int))]
        [InlineData("I.10000,di", PlcArea.IB, 10000, 1, typeof(int))]
        [InlineData("I.10000,di,10", PlcArea.IB, 10000, 10, typeof(int[]), typeof(int))]
        [InlineData("M.10000,x0,1", PlcArea.FB, 80000, 1, typeof(bool))]
        [InlineData("M.10000,x0", PlcArea.FB, 80000, 1, typeof(bool))]
        [InlineData("M.10000,b,1", PlcArea.FB, 10000, 1, typeof(byte))]
        [InlineData("M.10000,b", PlcArea.FB, 10000, 1, typeof(byte))]
        [InlineData("M.10000,b,10", PlcArea.FB, 10000, 10, typeof(byte[]), typeof(byte))]
        [InlineData("M.10000,c,1", PlcArea.FB, 10000, 1, typeof(char))]
        [InlineData("M.10000,c", PlcArea.FB, 10000, 1, typeof(char))]
        [InlineData("M.10000,c,10", PlcArea.FB, 10000, 10, typeof(char[]), typeof(char))]
        [InlineData("M.10000,w,1", PlcArea.FB, 10000, 1, typeof(ushort))]
        [InlineData("M.10000,w", PlcArea.FB, 10000, 1, typeof(ushort))]
        [InlineData("M.10000,w,10", PlcArea.FB, 10000, 10, typeof(ushort[]), typeof(ushort))]
        [InlineData("M.10000,i,1", PlcArea.FB, 10000, 1, typeof(short))]
        [InlineData("M.10000,i", PlcArea.FB, 10000, 1, typeof(short))]
        [InlineData("M.10000,i,10", PlcArea.FB, 10000, 10, typeof(short[]), typeof(short))]
        [InlineData("M.10000,dw,1", PlcArea.FB, 10000, 1, typeof(uint))]
        [InlineData("M.10000,dw", PlcArea.FB, 10000, 1, typeof(uint))]
        [InlineData("M.10000,dw,10", PlcArea.FB, 10000, 10, typeof(uint[]), typeof(uint))]
        [InlineData("M.10000,di,1", PlcArea.FB, 10000, 1, typeof(int))]
        [InlineData("M.10000,di", PlcArea.FB, 10000, 1, typeof(int))]
        [InlineData("M.10000,di,10", PlcArea.FB, 10000, 10, typeof(int[]), typeof(int))]
        [InlineData("Q.10000,x0,1", PlcArea.QB, 80000, 1, typeof(bool))]
        [InlineData("Q.10000,x0", PlcArea.QB, 80000, 1, typeof(bool))]
        [InlineData("Q.10000,b,1", PlcArea.QB, 10000, 1, typeof(byte))]
        [InlineData("Q.10000,b", PlcArea.QB, 10000, 1, typeof(byte))]
        [InlineData("Q.10000,b,10", PlcArea.QB, 10000, 10, typeof(byte[]), typeof(byte))]
        [InlineData("Q.10000,c,1", PlcArea.QB, 10000, 1, typeof(char))]
        [InlineData("Q.10000,c", PlcArea.QB, 10000, 1, typeof(char))]
        [InlineData("Q.10000,c,10", PlcArea.QB, 10000, 10, typeof(char[]), typeof(char))]
        [InlineData("Q.10000,w,1", PlcArea.QB, 10000, 1, typeof(ushort))]
        [InlineData("Q.10000,w", PlcArea.QB, 10000, 1, typeof(ushort))]
        [InlineData("Q.10000,w,10", PlcArea.QB, 10000, 10, typeof(ushort[]), typeof(ushort))]
        [InlineData("Q.10000,i,1", PlcArea.QB, 10000, 1, typeof(short))]
        [InlineData("Q.10000,i", PlcArea.QB, 10000, 1, typeof(short))]
        [InlineData("Q.10000,i,10", PlcArea.QB, 10000, 10, typeof(short[]), typeof(short))]
        [InlineData("Q.10000,dw,1", PlcArea.QB, 10000, 1, typeof(uint))]
        [InlineData("Q.10000,dw", PlcArea.QB, 10000, 1, typeof(uint))]
        [InlineData("Q.10000,dw,10", PlcArea.QB, 10000, 10, typeof(uint[]), typeof(uint))]
        [InlineData("Q.10000,di,1", PlcArea.QB, 10000, 1, typeof(int))]
        [InlineData("Q.10000,di", PlcArea.QB, 10000, 1, typeof(int))]
        [InlineData("Q.10000,di,10", PlcArea.QB, 10000, 10, typeof(int[]), typeof(int))]
        public void TryParseTagTests(string tag, PlcArea resultArea, int offset, int length, Type resultType, Type vartype = null)
        {
            if (vartype == null)
            {
                vartype = resultType;
            }

            Assert.True(TagParser.TryParseTag(tag, out TagParser.TagParserResult result));
            Assert.Equal(resultArea, result.Area);
            Assert.Equal(offset, result.Offset);
            Assert.Equal(length, result.Length);
            Assert.Equal(resultType, result.ResultType);
            Assert.Equal(vartype, result.VarType);
        }




        [Fact]
        public void TryParseInputBitWithLength()
        {
            Assert.True(TagParser.TryParseTag("I.1910,x3,1", out TagParser.TagParserResult result));
            Assert.Equal(PlcArea.IB, result.Area);
            Assert.Equal(15283, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }

        [Fact]
        public void TryParseInputBitWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("I.1910,x3", out TagParser.TagParserResult result));
            Assert.Equal(PlcArea.IB, result.Area);
            Assert.Equal(15283, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }


        [Fact]
        public void ParseExceptions()
        {

            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("DB.10000,di,10"));
            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("1.10000,di,10"));
            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("i1.10000,di,10"));
            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("e1.10000,di,10"));
            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("m1.10000,di,10"));
            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("q1.10000,di,10"));
            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("M1.10000,di,10"));

            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("M1.di,10"));
            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("M.1,10"));
            Assert.Throws<Dacs7TagParserException>(() => TagParser.ParseTag("M.1,10"));

        }

    }
}
