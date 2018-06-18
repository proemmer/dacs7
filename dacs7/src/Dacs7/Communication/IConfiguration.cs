using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Communication
{
    public interface IConfiguration
    {
        int ReceiveBufferSize { get; set; }
        int AutoconnectTime { get; set; }
    }
}
