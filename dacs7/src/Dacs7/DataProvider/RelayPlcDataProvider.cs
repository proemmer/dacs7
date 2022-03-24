// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.ReadWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.DataProvider
{
    public class RelayPlcDataProvider : IPlcDataProvider
    {
        private static readonly Lazy<RelayPlcDataProvider> _default = new(() => new RelayPlcDataProvider());
        private Dacs7Client _client;


        public static RelayPlcDataProvider Instance => _default.Value;


        public void UseClient(Dacs7Client client)
        {
            _client = client;
        }

        public async Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems)
        {
            IEnumerable<string> reads = readItems.Select(ri => ri.ToTag());
            IEnumerable<DataValue> readResult = await _client.ReadAsync(reads).ConfigureAwait(false);

            List<ReadRequestItem>.Enumerator enumerator = readItems.GetEnumerator();
            List<ReadResultItem> result = new();
            foreach (DataValue item in readResult)
            {
                if (!enumerator.MoveNext())
                {
                    break;
                }

                result.Add(new ReadResultItem(enumerator.Current, item.ReturnCode, item.Data));
            }
            return result;
        }


        public async Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems)
        {
            IEnumerable<KeyValuePair<string, object>> writes = writeItems.Select(ri => new KeyValuePair<string, object>(ri.ToTag(), ri.Data));
            IEnumerable<ItemResponseRetValue> writeResult = await _client.WriteAsync(writes).ConfigureAwait(false);

            List<WriteRequestItem>.Enumerator enumerator = writeItems.GetEnumerator();
            List<WriteResultItem> result = new();
            foreach (ItemResponseRetValue item in writeResult)
            {
                if (!enumerator.MoveNext())
                {
                    break;
                }

                result.Add(new WriteResultItem(enumerator.Current, item));
            }
            return result;
        }
    }
}
