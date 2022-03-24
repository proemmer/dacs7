﻿using Dacs7;
using Dacs7.ReadWrite;
using Dacs7Cli.Options;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dacs7Cli
{
    internal static class TaskReadCommand
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("taskread", cmd =>
            {
                cmd.Description = "Read tags from the plc.";

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
                    ReadOptions readOptions = null; ;
                    try
                    {
                        readOptions = new ReadOptions
                        {
                            Debug = debugOption.HasValue(),
                            Trace = traceOption.HasValue(),
                            Address = addressOption.HasValue() ? addressOption.Value() : "localhost",
                            RegisterItems = registerOption.HasValue(),
                            Loops = loopsOption.HasValue() ? int.Parse(loopsOption.Value()) : 1,
                            Wait = waitOption.HasValue() ? int.Parse(waitOption.Value()) : 0,
                            Tags = tagsArguments.Values,
                            MaxJobs = maxJobsOption.HasValue() ? int.Parse(maxJobsOption.Value()) : 10,
                        }.Configure();
                        int result = await Read(readOptions, readOptions.LoggerFactory);

                        await Task.Delay(500);

                        return result;
                    }
                    finally
                    {
                        readOptions?.LoggerFactory?.Dispose();
                    }
                });
            });
        }



        private static async Task<int> Read(ReadOptions readOptions, ILoggerFactory loggerFactory)
        {
            Dacs7Client client = new(readOptions.Address, PlcConnectionType.Pg, 5000, loggerFactory)
            {
                MaxAmQCalled = (ushort)readOptions.MaxJobs,
                MaxAmQCalling = (ushort)readOptions.MaxJobs
            };
            ILogger logger = loggerFactory?.CreateLogger("Dacs7Cli.Read");

            try
            {
                await client.ConnectAsync();

                if (readOptions.RegisterItems)
                {
                    await client.RegisterAsync(readOptions.Tags);
                }

                Stopwatch swTotal = new();
                List<Task<IEnumerable<DataValue>>> tasks = new();
                for (int i = 0; i < readOptions.Loops; i++)
                {
                    try
                    {
                        tasks.Add(client.ReadAsync(readOptions.Tags));
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError($"Exception in loop {ex.Message}.");
                    }
                }

                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                logger?.LogError($"An error occurred in Read: {ex.Message} - {ex.InnerException?.Message}");
                return 1;
            }
            finally
            {
                if (readOptions.RegisterItems)
                {
                    await client.UnregisterAsync(readOptions.Tags);
                }

                await client.DisconnectAsync();
            }

            return 0;
        }

        public static long ElapsedNanoSeconds(long ticks)
        {
            return ticks * 1000000000 / Stopwatch.Frequency;
        }

        private static string GetValue(object v)
        {
            if (v is string s)
            {
                return s;
            }
            if (v is char[] c)
            {
                return new string(c);
            }
            if (v != null)
            {
                return v.ToString();
            }

            return "[null]";
        }

    }
}
