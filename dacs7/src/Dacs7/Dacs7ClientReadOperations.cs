using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.ReadWrite
{
    public static class Dacs7ClientReadExtensions
    {

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
            var readItems = values as IList<ReadItem> ?? values.ToList();
            var result = await client.ProtocolHandler.ReadAsync(readItems);
            var enumerator = readItems.GetEnumerator();
            return result.Select(value =>
            {
                enumerator.MoveNext();
                return new DataValue(enumerator.Current, value);
            }).ToList();
        }


        private static  IEnumerable<ReadItem> CreateNodeIdCollection(this Dacs7Client client, IEnumerable<string> values)
        {
            return new List<ReadItem>(values.Select(item => client.RegisteredOrGiven(item)));
        }

    }
}
