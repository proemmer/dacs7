using System.Collections.Generic;

namespace Dacs7Cli.Options
{
    public class ServerOptions : OptionsBase
    {
        public int Port { get; set; }
        public List<string> Tags { get; set; }

        public string DataProvider { get; set; }
        public ushort MaxPduSize { get; set; }
    }
}
