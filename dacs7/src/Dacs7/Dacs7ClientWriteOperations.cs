// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.ReadWrite
{
    public static class Dacs7ClientWriteExtensions
    {

        /// <summary>
        /// The maximum write item length of a single telegram.
        /// </summary>
        public static ushort GetWriteItemMaxLength(this Dacs7Client client) => client.S7Context != null ? client.S7Context.WriteItemMaxLength : (ushort)0;

        /// <summary>
        /// Takes a list of <see cref="KeyValuePair{string, object}"/> an tries to write them to the plc.
        /// Where the string is the variable name and the object is the data to write
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public static Task<IEnumerable<ItemResponseRetValue>> WriteAsync(this Dacs7Client client, params KeyValuePair<string, object>[] values) => client.WriteAsync(values as IEnumerable<KeyValuePair<string, object>>);

        /// <summary>
        /// Takes a list of <see cref="WriteItem"/> an tries to write them to the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="WriteItem"/>.</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public static Task<IEnumerable<ItemResponseRetValue>> WriteAsync(this Dacs7Client client, params WriteItem[] values) => client.WriteAsync(values as IEnumerable<WriteItem>);

        /// <summary>
        /// Takes a list of <see cref="KeyValuePair{string, object}"/> an tries to write them to the plc.
        /// Where the string is the variable name and the object is the data to write
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public static Task<IEnumerable<ItemResponseRetValue>> WriteAsync(this Dacs7Client client, IEnumerable<KeyValuePair<string, object>> values) => client.WriteAsync(client.CreateWriteNodeIdCollection(values));

        /// <summary>
        /// Takes a list of <see cref="WriteItem"/> an tries to write them to the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="WriteItem"/>.</param>
        /// <returns>returns an enumerable of <see cref="ItemResponseRetValue"/>, which containing the write results.</returns>
        public static Task<IEnumerable<ItemResponseRetValue>> WriteAsync(this Dacs7Client client, IEnumerable<WriteItem> values)
        {
            var writeItems = values as IList<WriteItem> ?? new List<WriteItem>(values);
            return client.ProtocolHandler.WriteAsync(writeItems);
        }





        private static IEnumerable<WriteItem> CreateWriteNodeIdCollection(this Dacs7Client client, IEnumerable<KeyValuePair<string, object>> values)
        {
            return new List<WriteItem>(values.Select(item =>
            {
                var result = client.RegisteredOrGiven(item.Key).Clone();
                result.Data = result.ConvertDataToMemory(item.Value);
                return WriteItem.NormalizeAndValidate(result);
            }));
        }

    }
}
