using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7
{
    public static class ItemExtensions
    {

        public static WriteItem From(this ReadItem ri, Memory<byte> data)
        {
            var result = ri.Clone();
            result.Data = data;
            return result;
        }
    }
}
