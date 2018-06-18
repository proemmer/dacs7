using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Communication
{
    public class S7OnlineConfiguration : IConfiguration
    {
        public int ReceiveBufferSize { get; set; } = 1024;  // buffer size to use for each socket I/O operation 
        public int AutoconnectTime { get; set; } = 5000; // <= 0 means disabled
    }
}
