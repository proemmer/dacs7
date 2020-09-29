using Dacs7.ReadWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.DataProvider
{
    public class RelayPlcDataProvider : IPlcDataProvider
    {
        private static readonly Lazy<RelayPlcDataProvider> _default = new Lazy<RelayPlcDataProvider>(() => new RelayPlcDataProvider());
        private Dacs7Client _client;


        public static RelayPlcDataProvider Instance => _default.Value;


        public void UseClient(Dacs7Client client) => _client = client;


        public async Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems)
        {
            var reads = readItems.Select(ri => ri.ToTag());
            var readResult = await _client.ReadAsync(reads).ConfigureAwait(false);

            var enumerator = readItems.GetEnumerator();
            var result = new List<ReadResultItem>();
            foreach (var item in readResult)
            {
                if (!enumerator.MoveNext()) break;
                result.Add(new ReadResultItem(enumerator.Current, item.ReturnCode, item.Data));
            }
            return result;
        }


        public async Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems)
        {
            var writes = writeItems.Select(ri => new KeyValuePair<string, object>(ri.ToTag(), ri.Data));
            var writeResult = await _client.WriteAsync(writes).ConfigureAwait(false);

            var enumerator = writeItems.GetEnumerator();
            var result = new List<WriteResultItem>();
            foreach (var item in writeResult)
            {
                if (!enumerator.MoveNext()) break;
                result.Add(new WriteResultItem(enumerator.Current, item));
            }
            return result;
        }
    }
}
