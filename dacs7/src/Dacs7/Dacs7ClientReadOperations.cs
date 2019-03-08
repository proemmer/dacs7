// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.ReadWrite
{
    public static class Dacs7ClientReadExtensions
    {
        ///// <summary>
        ///// The maximum read item length of a single telegram.
        ///// </summary>
        public static ushort GetReadItemMaxLength(this Dacs7Client client) => client.S7Context != null ? client.S7Context.ReadItemMaxLength : (ushort)0;

        /// <summary>
        /// Reads data from the plc.
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the read values</returns>
        public static Task<IEnumerable<DataValue>> ReadAsync(this Dacs7Client client, params string[] values) => client.ReadAsync(values as IEnumerable<string>);

        /// <summary>
        /// Reads data from the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="ReadItem"/></param>
        /// <returns>returns a enumerable with the read values</returns>
        public static Task<IEnumerable<DataValue>> ReadAsync(this Dacs7Client client, params ReadItem[] values) => client.ReadAsync(values as IEnumerable<ReadItem>);

        /// <summary>
        /// Reads data from the plc.
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the read values</returns>
        public static Task<IEnumerable<DataValue>> ReadAsync(this Dacs7Client client, IEnumerable<string> values) => client.ReadAsync(client.CreateNodeIdCollection(values));

        /// <summary>
        /// Reads data from the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="ReadItem"/></param>
        /// <returns>returns a enumerable with the read values</returns>
        public static async Task<IEnumerable<DataValue>> ReadAsync(this Dacs7Client client, IEnumerable<ReadItem> values)
        {
            var readItems = values as IList<ReadItem> ?? new List<ReadItem>(values);
            var result = await client.ProtocolHandler.ReadAsync(readItems);
            return new List<DataValue>(result.Select((entry) => new DataValue(entry.Key, entry.Value)));
        }


        internal static IEnumerable<ReadItem> CreateNodeIdCollection(this Dacs7Client client, IEnumerable<string> values)
            => new List<ReadItem>(values.Select(item => client.RegisteredOrGiven(item)));

    }
}
