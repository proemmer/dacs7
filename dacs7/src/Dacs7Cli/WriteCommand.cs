using Dacs7;
using Dacs7.ReadWrite;
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

                CommandOption addressOption = cmd.Option("-a | --address", "The IPAddress of the plc", CommandOptionType.SingleValue);
                CommandOption debugOption = cmd.Option("-d | --debug", "Activate debug output", CommandOptionType.NoValue);
                CommandOption traceOption = cmd.Option("-t | --trace", "Trace also dacs7 internals", CommandOptionType.NoValue);
                CommandOption registerOption = cmd.Option("-r | --register", "Register items for fast performing.", CommandOptionType.NoValue);
                CommandOption loopsOption = cmd.Option("-l | --loops", "Specify the number of read loops.", CommandOptionType.SingleValue);
                CommandOption waitOption = cmd.Option("-s | --wait", "Wait time between loops in ms.", CommandOptionType.SingleValue);
                CommandOption maxJobsOption = cmd.Option("-j | --jobs", "Maximum number of concurrent jobs.", CommandOptionType.SingleValue);

                CommandArgument tagsArguments = cmd.Argument("tags", "Tags to read.", true);

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
                            Tags = tagsArguments.Values,
                            MaxJobs = maxJobsOption.HasValue() ? int.Parse(maxJobsOption.Value()) : 10,
                        }.Configure();

                        int result = await Write(writeOptions, writeOptions.LoggerFactory);
                        await Task.Delay(500);

                        return result;
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
            Dacs7Client client = new(writeOptions.Address, PlcConnectionType.Pg, 5000, loggerFactory)
            {
                MaxAmQCalled = (ushort)writeOptions.MaxJobs,
                MaxAmQCalling = (ushort)writeOptions.MaxJobs
            };
            ILogger logger = loggerFactory?.CreateLogger("Dacs7Cli.Write");
            try
            {
                await client.ConnectAsync();

                List<KeyValuePair<string, object>> write = writeOptions.Tags.Select(x =>
                {
                    string[] s = x.Split('=');
                    return KeyValuePair.Create<string, object>(s[0], s[1]);
                }
                ).ToList();

                IEnumerable<ItemResponseRetValue> results = await client.WriteAsync(write);
                IEnumerator<ItemResponseRetValue> resultEnumerator = results.GetEnumerator();
                foreach (KeyValuePair<string, object> item in write)
                {
                    if (resultEnumerator.MoveNext())
                    {
                        logger?.LogInformation($"Write: {item.Key}={item.Value}  result: {resultEnumerator.Current}");
                    }
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
