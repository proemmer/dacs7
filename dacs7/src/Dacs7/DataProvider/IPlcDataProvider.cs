using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7.DataProvider
{

    public interface IPlcDataProvider
    {
        Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems);
        Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems);
    }
}
