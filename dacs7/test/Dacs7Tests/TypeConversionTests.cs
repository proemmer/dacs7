using System;
using System.Globalization;
using Xunit;

namespace Dacs7.Tests
{
    public class TypeConversionTests
    {
        [Fact()]
        public void ConvertTest()
        {
            Assert.Equal(45, GetValue<short>("+00045"));
            Assert.Equal(-45, GetValue<short>("-00045"));
            Assert.Equal(100, GetValue<short>("000100"));

            Assert.Equal(-99.2, GetValue<float>("-099.2"), 2);
            Assert.Equal(3.123, GetValue<float>("+003.123"), 2);

        }






        private T GetValue<T>(string s)
        {
            return (T)Convert.ChangeType(s, typeof(T), CultureInfo.InvariantCulture);
        }
    }
}
