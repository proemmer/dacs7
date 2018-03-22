using CommandLine;


namespace Dacs7Cli.Options
{
    public abstract class OptionsBase
    {
        [Option('d', "debug", HelpText = "Activate debug logging")]
        public bool Debug { get; set; }

        [Option('a', "address", HelpText = "Server Name or IP", Required = true)]
        public string Address { get; set; } = "localhost";


    }
}
