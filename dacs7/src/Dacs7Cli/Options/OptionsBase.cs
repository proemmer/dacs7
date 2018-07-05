using Microsoft.Extensions.Logging;

namespace Dacs7Cli.Options
{
    public abstract class OptionsBase
    {
        public bool Debug { get; set; }

        public string Address { get; set; } = "localhost";


        public bool Trace { get; set; }



        internal ILoggerFactory LoggerFactory { get; set; }
    }
}
