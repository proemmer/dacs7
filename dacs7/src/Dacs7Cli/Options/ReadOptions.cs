using CommandLine;
using System.Collections.Generic;

namespace Dacs7Cli.Options
{
    [Verb("read", HelpText = "Read data from opc ua server.")]
    public class ReadOptions : OptionsBase
    {
        [Option('t', "tags", Separator = ';', HelpText = "Tags to read.", Required = true)]
        public IList<string> Tags { get; set; }

        [Option('r', "register", HelpText = "Register items for fast performing.")]
        public bool RegisterItems { get; set; }

        [Option('l', "loops", HelpText = "Specify the number of read loops.")]
        public int Loops { get; set; } = 1;
    }
}
