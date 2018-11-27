using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {

        public async Task<IList<IPlcAlarm>> ReadPendingAlarmsAsync()
        {
            var result = await _protocolHandler.ReadPendingAlarmsAsync();
            if (result != null)
            {

            }
            return null;
        }
    }
}
