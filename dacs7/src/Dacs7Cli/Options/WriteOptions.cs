using CommandLine;
using System.Collections.Generic;

namespace Dacs7Cli.Options
{
    [Verb("write", HelpText = "Write data to opc ua server.")]
    public class WriteOptions : OptionsBase
    {
        [Option('t', "tags", Separator = ';', HelpText = "Tags to write.", Required = true)]
        public IList<string> Tags { get; set; }
    }
}
