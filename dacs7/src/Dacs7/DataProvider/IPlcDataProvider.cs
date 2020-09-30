// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7.DataProvider
{

    public interface IPlcDataProvider
    {
        /// <summary>
        /// The server will invoke this method if a read request is received and injects a list of <see cref="ReadRequestItem"/>.
        /// </summary>
        /// <param name="readItems">These items contains the requested data to read.</param>
        /// <returns>a list of <see cref="ReadResultItem"/></returns>
        Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems);

        /// <summary>
        /// The server will invoke this method if a write request is received and injects a list of <see cref="WriteRequestItem"/>.
        /// </summary>
        /// <param name="writeItems">These items contains the requested data to write.</param>
        /// <returns>a list of <see cref="WriteResultItem"/></returns>
        Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems);
    }
}
