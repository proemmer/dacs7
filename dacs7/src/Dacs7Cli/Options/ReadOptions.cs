using System.Collections.Generic;

namespace Dacs7Cli.Options
{
    public class ReadOptions : OptionsBase
    {
        public IList<string> Tags { get; set; }

        public bool RegisterItems { get; set; }

        public int Loops { get; set; } = 1;


        public int Wait { get; set; } = 0;
    }
}
