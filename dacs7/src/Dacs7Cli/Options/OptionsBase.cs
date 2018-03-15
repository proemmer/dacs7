using CommandLine;


namespace Dacs7Cli.Options
{
    public abstract class OptionsBase
    {
        [Option('d', "debug", HelpText = "Activate debug logging")]
        public bool Debug { get; set; }

        [Option('a', "address", HelpText = "Server Name or IP", Required = true)]
        public string Address { get; set; } = "localhost";

        [Option('p', "port", HelpText = "Port")]
        public int Port { get; set; } = 102;


        [Option('r', "rack", HelpText = "Rack")]
        public int Rack { get; set; } = 0;

        [Option('s', "slot", HelpText = "Slot")]
        public int Slot { get; set; } = 0;

        public string ConnectionString => $"Data Source={Address}:{Port},{Rack},{Slot}";
    }
}
