using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {

        /// <summary>
        /// Reads data from the plc.
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the read values</returns>
        public Task<IEnumerable<DataValue>> ReadAsync(params string[] values) => ReadAsync(values as IEnumerable<string>);

        /// <summary>
        /// Reads data from the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="ReadItem"/></param>
        /// <returns>returns a enumerable with the read values</returns>
        public Task<IEnumerable<DataValue>> ReadAsync(params ReadItem[] values) => ReadAsync(values as IEnumerable<ReadItem>);

        /// <summary>
        /// Reads data from the plc.
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the read values</returns>
        public Task<IEnumerable<DataValue>> ReadAsync(IEnumerable<string> values) => ReadAsync(CreateNodeIdCollection(values));

        /// <summary>
        /// Reads data from the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="ReadItem"/></param>
        /// <returns>returns a enumerable with the read values</returns>
        public async Task<IEnumerable<DataValue>> ReadAsync(IEnumerable<ReadItem> values)
        {
            var result = await _protocolHandler.ReadAsync(values);
            var enumerator = values.GetEnumerator();
            return result.Select(value =>
            {
                enumerator.MoveNext();
                return new DataValue(enumerator.Current, value);
            }).ToList();
        }

    }
}
