using Dacs7;
using Dacs7Cli.Options;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dacs7Cli
{
    public static class ServeCommand
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("serve", cmd =>
            {
                cmd.Description = "Serve a plc simulation.";

                var addressOption = cmd.Option("-a | --address", "The IPAddress of the plc", CommandOptionType.SingleValue);
                var portOption = cmd.Option("-p | --port", "The port of the plc", CommandOptionType.SingleValue);
                var debugOption = cmd.Option("-d | --debug", "Activate debug output", CommandOptionType.NoValue);
                var traceOption = cmd.Option("-t | --trace", "Trace also dacs7 internals", CommandOptionType.NoValue);
                var maxJobsOption = cmd.Option("-j | --jobs", "Maximum number of concurrent jobs.", CommandOptionType.SingleValue);

                var tagsArguments = cmd.Argument("tags", "Tags to read.", true);

                cmd.OnExecute(async () =>
                {
                    ServerOptions options = null; ;
                    try
                    {
                        options = new ServerOptions
                        {
                            Debug = debugOption.HasValue(),
                            Trace = traceOption.HasValue(),
                            Address = addressOption.HasValue() ? addressOption.Value() : "localhost",
                            MaxJobs = maxJobsOption.HasValue() ? int.Parse(maxJobsOption.Value()) : 10,
                            Port = portOption.HasValue() ? int.Parse(portOption.Value()) : 102
                        }.Configure();
                        var result = await Serve(options, options.LoggerFactory);

                        await Task.Delay(500);

                        return result;
                    }
                    finally
                    {
                        options?.LoggerFactory?.Dispose();
                    }
                });
            });
        }



        private static async Task<int> Serve(ServerOptions options, ILoggerFactory loggerFactory)
        {
            var server = new Dacs7Server(options.Port, 5000, loggerFactory, SimulationPlcDataProvider.Instance)
            {
                MaxAmQCalled = (ushort)options.MaxJobs,
                MaxAmQCalling = (ushort)options.MaxJobs
            };
            var logger = loggerFactory?.CreateLogger("Dacs7Cli.Serve");

            try
            {

                SimulationPlcDataProvider.Instance.Register(PlcArea.DB, 1000, 1);
                Console.WriteLine($"Started serving on port {options.Port} !");
                await server.ConnectAsync();

                Console.WriteLine("Please press any key to stop serving!");
                Console.ReadKey();

            }
            catch (Exception ex)
            {
                logger?.LogError($"An error occured in Serve: {ex.Message} - {ex.InnerException?.Message}");
                return 1;
            }
            finally
            {
                await server.DisconnectAsync();
            }

            return 0;
        }

    }
}
