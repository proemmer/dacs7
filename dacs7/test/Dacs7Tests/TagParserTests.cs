using Dacs7;
using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Dacs7Tests
{

    public class TagParserTests
    {
        [Fact]
        public void ParseBitWithLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,x0,1", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(80000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }

        [Fact]
        public void ParseBitWithoutLength()
        {
            Assert.True(TagParser.TryParseTag("DB1.10000,x0", out var result));
            Assert.Equal(PlcArea.DB, result.Area);
            Assert.Equal(80000, result.Offset);
            Assert.Equal(1, result.Length);
            Assert.Equal(typeof(bool), result.ResultType);
            Assert.Equal(typeof(bool), result.VarType);
        }
    }
}
