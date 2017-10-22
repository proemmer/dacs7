using System;
using System.Threading;

namespace Dacs7
{


    public partial class Dacs7Client
    {
        #region Helper class
        internal class CallbackHandler
        {
            public ushort Id { get; set; }
            public AutoResetEvent Event { get; set; }
            public IMessage ResponseMessage { get; set; }
            public Exception OccuredException { get; set; }
            public Action<IMessage> OnCallbackAction { get; set; }
        }
        #endregion
    }
}
