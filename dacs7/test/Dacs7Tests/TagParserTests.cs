using Dacs7;
using Dacs7.Domain;
using System;
using Xunit;

namespace Dacs7Tests
{

    public class TagParserTests
    {
        [Fact]
        public void ParseDbBitWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,x0,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(80000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }

        [Fact]
        public void ParseDbBitWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,x0", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(80000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }

        [Fact]
        public void ParseDbByteWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,b", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(byte), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void ParseDbByteWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,b,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(byte), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void ParseDbByteWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,b,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(byte[]), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void ParseDbCharWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,c", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(char), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void ParseDbCharWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,c,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(char), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void ParseDbCharWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,c,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(char[]), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void ParseDbUShortWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,w", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(ushort), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }

        [Fact]
        public void ParseDbUShortWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,w,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(ushort), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }

        [Fact]
        public void ParseDbUShortWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,w,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(ushort[]), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }


        [Fact]
        public void ParseDbShortWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,i", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(short), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void ParseDbShortWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,i,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(short), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void ParseDbShortWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,i,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(short[]), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void ParseDbUIntWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,dw", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(UInt32), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }

        [Fact]
        public void ParseDbUIntWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,dw,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(UInt32), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }

        [Fact]
        public void ParseDbUIntWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,dw,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(UInt32[]), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }


        [Fact]
        public void ParseDbIntWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,di", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(Int32), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }

        [Fact]
        public void ParseDbIntWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,di,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(Int32), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }

        [Fact]
        public void ParseDbIntWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,di,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(Int32[]), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }

    }
}
