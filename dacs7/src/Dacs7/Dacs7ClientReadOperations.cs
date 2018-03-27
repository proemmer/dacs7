using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {
        public Task<IEnumerable<object>> ReadAsync(params string[] values) => ReadAsync(values as IEnumerable<string>);

        public async Task<IEnumerable<object>> ReadAsync(IEnumerable<string> values)
        {
            var items = CreateNodeIdCollection(values);
            var result = await _protocolHandler.ReadAsync(items);
            var enumerator = items.GetEnumerator();
            return result.Select(value =>
            {
                enumerator.MoveNext();
                return Convert.ChangeType(value, enumerator.Current.ResultType);
            }).ToList();
        }


        public async Task<Memory<byte>> ReadAsync(int dbNumber, int offset, int length)
        {
            Memory<byte> result = Memory<byte>.Empty;
            
            var bytesToRead = length;
            var processed = 0;
            while (bytesToRead > 0)
            {
                var slice = Math.Min(_s7Context.ReadItemMaxLength, bytesToRead);
                if (bytesToRead == length && slice != length)
                {
                    result = new byte[length];
                }

                Memory<byte> partResult = (await ReadAsync($"db{dbNumber}.{offset + processed},b,{slice}")).FirstOrDefault() as byte[];

                if(result.IsEmpty)
                {
                    result = partResult;
                }
                else
                {
                    partResult.CopyTo(result.Slice(processed));
                }

                processed += slice;
                bytesToRead -= slice;
            }

            return result;
        }

    }
}
