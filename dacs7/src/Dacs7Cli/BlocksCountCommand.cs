using Dacs7;
using Dacs7.Metadata;
using Dacs7Cli.Options;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7Cli
{
    internal static class BlocksCountCommand
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("blockscount", cmd =>
            {
                cmd.Description = "determine blockinfo from the plc.";

                CommandOption addressOption = cmd.Option("-a | --address", "The IPAddress of the plc", CommandOptionType.SingleValue);
                CommandOption debugOption = cmd.Option("-d | --debug", "Activate debug output", CommandOptionType.NoValue);
                CommandOption traceOption = cmd.Option("-t | --trace", "Trace also dacs7 internals", CommandOptionType.NoValue);
                CommandOption maxJobsOption = cmd.Option("-j | --jobs", "Maximum number of concurrent jobs.", CommandOptionType.SingleValue);

                cmd.OnExecute(async () =>
                {
                    ReadOptions options = null; ;
                    try
                    {
                        options = new ReadOptions
                        {
                            Debug = debugOption.HasValue(),
                            Trace = traceOption.HasValue()
                        }.Configure();
                        int result = await Read(addressOption.HasValue() ? addressOption.Value() : "localhost",
                                                maxJobsOption.HasValue() ? ushort.Parse(maxJobsOption.Value()) : (ushort)10,
                                                options.LoggerFactory);

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



        private static async Task<int> Read(string address, ushort maxJobs, ILoggerFactory loggerFactory)
        {
            Dacs7Client client = new(address, PlcConnectionType.Pg, 5000, loggerFactory)
            {
                MaxAmQCalled = maxJobs,
                MaxAmQCalling = maxJobs
            };
            ILogger logger = loggerFactory?.CreateLogger("Dacs7Cli.BlokcsCount");

            try
            {

                await client.ConnectAsync();

                IPlcBlocksCount result = await client.ReadBlocksCountAsync();

                if (result != null)
                {
                    logger?.LogInformation($"FB: {result.Fb}");
                    logger?.LogInformation($"FC: {result.Fc}");
                    logger?.LogInformation($"DB: {result.Db}");
                }
                else
                {
                    logger?.LogError($"No result on blockinfo");
                }

            }
            catch (Exception ex)
            {
                logger?.LogError($"An error occured in Read: {ex.Message} - {ex.InnerException?.Message}");
                return 1;
            }
            finally
            {

                await client.DisconnectAsync();
            }

            return 0;
        }

        private static (PlcBlockType blockType, int number) TranslateFromInput(string input)
        {
            string type = input.Substring(0, input.Count(x => x >= 'A' && x <= 'Z' || x >= 'a' && x <= 'z')).ToUpper();
            string number = input.Substring(type.Length).Trim();
            if (int.TryParse(number, out int blockNumber))
            {
                return (Enum.Parse<PlcBlockType>(type, true), blockNumber);
            }
            throw new ArgumentException(nameof(input));
        }

    }
}
