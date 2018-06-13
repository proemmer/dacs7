using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace Dacs7Tests
{
    public class TypeConversionTests
    {
        [Fact()]
        public void ConvertTest()
        {
            Assert.Equal(45, GetValue<short>("+00045"));
            Assert.Equal(-45, GetValue<short>("-00045"));
            Assert.Equal(100, GetValue<short>("000100"));

            Assert.Equal(-99.2, GetValue<Single>("-099.2"), 2);
            Assert.Equal(3.123, GetValue<Single>("+003.123"), 2);
            
        }






        private T GetValue<T>(string s)
        {
            return (T)Convert.ChangeType(s, typeof(T), CultureInfo.InvariantCulture);
        }
    }
}
