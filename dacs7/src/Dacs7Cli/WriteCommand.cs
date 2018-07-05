using Dacs7;
using Dacs7Cli.Options;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7Cli
{
    internal static class WriteCommand
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("write", cmd =>
            {
                cmd.Description = "Write tags to the plc.";

                var addressOption = cmd.Option("-a | --address", "The IPAddress of the plc", CommandOptionType.SingleValue);
                var debugOption = cmd.Option("-d | --debug", "Activate debug output", CommandOptionType.NoValue);
                var traceOption = cmd.Option("-t | --trace", "Trace also dacs7 internals", CommandOptionType.NoValue);
                var registerOption = cmd.Option("-r | --register", "Register items for fast performing.", CommandOptionType.NoValue);
                var loopsOption = cmd.Option("-l | --loops", "Specify the number of read loops.", CommandOptionType.SingleValue);
                var waitOption = cmd.Option("-s | --wait", "Wait time between loops in ms.", CommandOptionType.SingleValue);

                var tagsArguments = cmd.Argument("tags", "Tags to read.", true);

                cmd.OnExecute(async () =>
                {
                    WriteOptions writeOptions = null;
                    try
                    {
                        writeOptions = new WriteOptions
                        {
                            Debug = debugOption.HasValue(),
                            Trace = traceOption.HasValue(),
                            Address = addressOption.HasValue() ? addressOption.Value() : "localhost",
                            //RegisterItems = registerOption.HasValue(),
                            //Loops = loopsOption.HasValue() ? Int32.Parse(loopsOption.Value()) : 1,
                            //Wait = waitOption.HasValue() ? Int32.Parse(waitOption.Value()) : 0,
                            Tags = tagsArguments.Values
                        }.Configure();
                        return await Write(writeOptions, writeOptions.LoggerFactory);
                    }
                    finally
                    {
                        writeOptions?.LoggerFactory.Dispose();
                    }
                });
            });
        }


        internal static async Task<int> Write(WriteOptions writeOptions, ILoggerFactory loggerFactory)
        {
            var client = new Dacs7Client(writeOptions.Address, PlcConnectionType.Pg, 5000, loggerFactory);
            var logger = loggerFactory?.CreateLogger("Dacs7Cli.Write");
            try
            {
                await client.ConnectAsync();

                var write = writeOptions.Tags.Select(x =>
                {
                    var s = x.Split('=');
                    return KeyValuePair.Create<string, object>(s[0], s[1]);
                }
                ).ToList();

                await client.WriteAsync(write);

                foreach (var item in write)
                {
                    logger?.LogInformation($"Write: {item.Key}={item.Value}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"An error occured in Write: {ex.Message} - {ex.InnerException?.Message}");
                return 1;
            }
            finally
            {
                await client.DisconnectAsync();
            }

            return 0;
        }

    }
}
