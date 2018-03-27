using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {
        public Task<IEnumerable<byte>> WriteAsync(params KeyValuePair<string, object>[] values) => WriteAsync(values as IEnumerable<KeyValuePair<string, object>>);

        public Task<IEnumerable<byte>> WriteAsync(IEnumerable<KeyValuePair<string, object>> values)
        {
            var items = CreateWriteNodeIdCollection(values);
            return _protocolHandler.WriteAsync(items);
        }


        public async Task<byte> WriteAsync(int dbNumber, int offset, Memory<byte> data)
        {
            Memory<byte> result = Memory<byte>.Empty;

            var bytesToWrite = data.Length;
            var processed = 0;
            while (bytesToWrite > 0)
            {
                var slice = Math.Min(_s7Context.WriteItemMaxLength, bytesToWrite);
                var erroCode = (await WriteAsync(new KeyValuePair<string,object>($"db{dbNumber}.{offset + processed},b,{slice}", data.Slice(processed, slice)))).FirstOrDefault();

                if (erroCode != 0xFF)
                    return erroCode;

                processed += slice;
                bytesToWrite -= slice;
            }

            return 0xFF;
        }

    }
}
