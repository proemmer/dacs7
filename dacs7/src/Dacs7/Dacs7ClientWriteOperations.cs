using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {

        /// <summary>
        /// Writes data from the plc.
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public Task<IEnumerable<ItemResponseRetValue>> WriteAsync(params KeyValuePair<string, object>[] values) => WriteAsync(values as IEnumerable<KeyValuePair<string, object>>);

        /// <summary>
        /// Writes data from the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="WriteItem"/>.</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public Task<IEnumerable<ItemResponseRetValue>> WriteAsync(params WriteItem[] values) => WriteAsync(values as IEnumerable<WriteItem>);

        /// <summary>
        /// Writes data from the plc.
        /// </summary>
        /// <param name="values">a list of tags with the following syntax Area.Offset,DataType[,length]</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public Task<IEnumerable<ItemResponseRetValue>> WriteAsync(IEnumerable<KeyValuePair<string, object>> values) => WriteAsync(CreateWriteNodeIdCollection(values));

        /// <summary>
        /// Writes data from the plc.
        /// </summary>
        /// <param name="values">a list of <see cref="WriteItem"/>.</param>
        /// <returns>returns a enumerable with the write result, 0xFF = Success</returns>
        public Task<IEnumerable<ItemResponseRetValue>> WriteAsync(IEnumerable<WriteItem> items)
        {
            return _protocolHandler.WriteAsync(items);
        }

    }
}
