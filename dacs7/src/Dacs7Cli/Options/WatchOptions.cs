using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7Cli.Options
{
    [Verb("watch", HelpText = "Watch opc items.")]
    public class WatchOptions : OptionsBase
    {
        [Option('t', "tags", Separator = ';', HelpText = "Tags to read.", Required = true)]
        public IList<string> Tags { get; set; }
    }
}
