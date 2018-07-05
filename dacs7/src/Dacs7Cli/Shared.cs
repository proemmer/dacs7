using Dacs7Cli.Options;
using Microsoft.Extensions.Logging;

namespace Dacs7Cli
{
    internal static class Shared
    {
        internal static T Configure<T>(this T options) where T : OptionsBase
        {
            options.LoggerFactory = new LoggerFactory()
                                            .WithFilter(new FilterLoggerSettings
                                                {
                                                    { "Microsoft", LogLevel.Warning },
                                                    { "System", LogLevel.Warning },
                                                    { "Dacs7", options.Trace ? LogLevel.Trace : LogLevel.Information }
                                                })
                                            .AddConsole(options.Debug ? LogLevel.Debug : LogLevel.Information);
            return options;
        }
    }
}
