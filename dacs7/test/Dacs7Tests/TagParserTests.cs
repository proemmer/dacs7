using Dacs7;
using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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

    }
}
