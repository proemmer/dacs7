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
        /// Writes data from the plc.
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public static Task<IEnumerable<ItemResponseRetValue>> WriteAsync(this Dacs7Client client, params KeyValuePair<string, object>[] values) => client.WriteAsync(values as IEnumerable<KeyValuePair<string, object>>);

        /// <summary>
        /// Writes data from the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="WriteItem"/>.</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public static  Task<IEnumerable<ItemResponseRetValue>> WriteAsync(this Dacs7Client client, params WriteItem[] values) => client.WriteAsync(values as IEnumerable<WriteItem>);

        /// <summary>
        /// Writes data from the plc.
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public static  Task<IEnumerable<ItemResponseRetValue>> WriteAsync(this Dacs7Client client, IEnumerable<KeyValuePair<string, object>> values) => client.WriteAsync(client.CreateWriteNodeIdCollection(values));

        /// <summary>
        /// Writes data from the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="WriteItem"/>.</param>
        /// <returns>returns a enumerable with the write result. <see cref="ItemResponseRetValue"/></returns>
        public static  Task<IEnumerable<ItemResponseRetValue>> WriteAsync(this Dacs7Client client, IEnumerable<WriteItem> values)
        {
            var writeItems = values as IList<WriteItem> ?? values.ToList();
            return client.ProtocolHandler.WriteAsync(writeItems);
        }





        private static IEnumerable<WriteItem> CreateWriteNodeIdCollection(this Dacs7Client client, IEnumerable<KeyValuePair<string, object>> values)
        {
            return new List<WriteItem>(values.Select(item =>
            {
                var result = client.RegisteredOrGiven(item.Key).Clone();
                result.Data = result.ConvertDataToMemory(item.Value);
                return result;
            }));
        }

    }
}
