using Dacs7;
using Dacs7.Domain;
using System;
using Xunit;

namespace Dacs7Tests
{

    public class TagParserTests
    {
        [Fact]
        public void TryParseDbBitWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,x0,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(80000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }

        [Fact]
        public void TryParseDbBitWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,x0", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(80000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }

        [Fact]
        public void TryParseDbByteWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,b", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(byte), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void TryParseDbByteWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,b,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(byte), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void TryParseDbByteWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,b,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(byte[]), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void TryParseDbCharWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,c", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(char), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void TryParseDbCharWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,c,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(char), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void TryParseDbCharWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,c,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(char[]), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void TryParseDbUShortWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,w", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(ushort), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }

        [Fact]
        public void TryParseDbUShortWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,w,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(ushort), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }

        [Fact]
        public void TryParseDbUShortWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,w,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(ushort[]), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }


        [Fact]
        public void TryParseDbShortWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,i", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(short), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void TryParseDbShortWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,i,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(short), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void TryParseDbShortWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,i,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(short[]), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void TryParseDbUIntWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,dw", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(UInt32), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }

        [Fact]
        public void TryParseDbUIntWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,dw,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(UInt32), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }

        [Fact]
        public void TryParseDbUIntWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,dw,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(UInt32[]), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }


        [Fact]
        public void TryParseDbIntWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,di", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(Int32), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }

        [Fact]
        public void TryParseDbIntWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,di,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(Int32), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }

        [Fact]
        public void TryParseDbIntWithSpecificLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,di,10", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(Int32[]), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }







        [Fact]
        public void ParseDbBitWithLength()
        {
            var result = TagParser.ParseTag("DB1.10000,x0,1");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(80000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }

        [Fact]
        public void ParseDbBitWithoutLength()
        {
            var result = TagParser.ParseTag("DB1.10000,x0");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(80000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }

        [Fact]
        public void ParseDbByteWithoutLength()
        {
            var result = TagParser.ParseTag("DB1.10000,b");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(byte), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void ParseDbByteWithLength()
        {
            var result = TagParser.ParseTag("DB1.10000,b,1");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(byte), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void ParseDbByteWithSpecificLength()
        {
            var result = TagParser.ParseTag("DB1.10000,b,10");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(byte[]), result.ResultType);
            Assert.Equal(typeof(byte), result.VarType);
        }

        [Fact]
        public void ParseDbCharWithoutLength()
        {
            var result = TagParser.ParseTag("DB1.10000,c");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(char), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void ParseDbCharWithLength()
        {
            var result = TagParser.ParseTag("DB1.10000,c,1");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(char), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void ParseDbCharWithSpecificLength()
        {
            var result = TagParser.ParseTag("DB1.10000,c,10");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(char[]), result.ResultType);
            Assert.Equal(typeof(char), result.VarType);
        }

        [Fact]
        public void ParseDbUShortWithoutLength()
        {
            var result = TagParser.ParseTag("DB1.10000,w");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(ushort), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }

        [Fact]
        public void ParseDbUShortWithLength()
        {
            var result = TagParser.ParseTag("DB1.10000,w,1");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(ushort), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }

        [Fact]
        public void ParseDbUShortWithSpecificLength()
        {
            var result = TagParser.ParseTag("DB1.10000,w,10");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(ushort[]), result.ResultType);
            Assert.Equal(typeof(ushort), result.VarType);
        }


        [Fact]
        public void ParseDbShortWithoutLength()
        {
            var result = TagParser.ParseTag("DB1.10000,i");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(short), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void ParseDbShortWithLength()
        {
            var result = TagParser.ParseTag("DB1.10000,i,1");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(short), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void ParseDbShortWithSpecificLength()
        {
            var result = TagParser.ParseTag("DB1.10000,i,10");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(short[]), result.ResultType);
            Assert.Equal(typeof(short), result.VarType);
        }

        [Fact]
        public void ParseDbUIntWithoutLength()
        {
            var result = TagParser.ParseTag("DB1.10000,dw");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(UInt32), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }

        [Fact]
        public void ParseDbUIntWithLength()
        {
            var result = TagParser.ParseTag("DB1.10000,dw,1");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(UInt32), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }

        [Fact]
        public void ParseDbUIntWithSpecificLength()
        {
            var result = TagParser.ParseTag("DB1.10000,dw,10");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(UInt32[]), result.ResultType);
            Assert.Equal(typeof(UInt32), result.VarType);
        }


        [Fact]
        public void ParseDbIntWithoutLength()
        {
            var result = TagParser.ParseTag("DB1.10000,di");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(Int32), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }

        [Fact]
        public void ParseDbIntWithLength()
        {
            var result = TagParser.ParseTag("DB1.10000,di,1");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(Int32), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }

        [Fact]
        public void ParseDbIntWithSpecificLength()
        {
            var result = TagParser.ParseTag("DB1.10000,di,10");
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(10000, result.Offset);
            Assert.Equal(10, result.Length);
            Assert.Equal(typeof(Int32[]), result.ResultType);
            Assert.Equal(typeof(Int32), result.VarType);
        }

    }
}
