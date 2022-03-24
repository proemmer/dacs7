using Dacs7;
using Dacs7.Alarms;
using Dacs7Cli.Options;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7Cli
{
    internal static class WatchAlarmsCommand
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("watchalarms", cmd =>
            {
                cmd.Description = "Read alarms from the plc.";

                CommandOption addressOption = cmd.Option("-a | --address", "The IPAddress of the plc", CommandOptionType.SingleValue);
                CommandOption debugOption = cmd.Option("-d | --debug", "Activate debug output", CommandOptionType.NoValue);
                CommandOption traceOption = cmd.Option("-t | --trace", "Trace also dacs7 internals", CommandOptionType.NoValue);
                CommandOption maxJobsOption = cmd.Option("-j | --jobs", "Maximum number of concurrent jobs.", CommandOptionType.SingleValue);

                cmd.OnExecute(async () =>
                {
                    ReadAlarmsOptions readOptions = null; ;
                    try
                    {
                        readOptions = new ReadAlarmsOptions
                        {
                            Debug = debugOption.HasValue(),
                            Trace = traceOption.HasValue(),
                            Address = addressOption.HasValue() ? addressOption.Value() : "localhost",
                            MaxJobs = maxJobsOption.HasValue() ? int.Parse(maxJobsOption.Value()) : 10,
                        }.Configure();
                        int result = await ReadAlarms(readOptions, readOptions.LoggerFactory);

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



        private static async Task<int> ReadAlarms(ReadAlarmsOptions readOptions, ILoggerFactory loggerFactory)
        {
            Dacs7Client client = new(readOptions.Address, PlcConnectionType.Pg, 5000, loggerFactory)
            {
                MaxAmQCalled = (ushort)readOptions.MaxJobs,
                MaxAmQCalling = (ushort)readOptions.MaxJobs
            };
            ILogger logger = loggerFactory?.CreateLogger("Dacs7Cli.ReadAlarms");

            try
            {
                long msTotal = 0;
                await client.ConnectAsync();

                try
                {
                    Stopwatch sw = new();
                    sw.Start();
                    CancellationTokenSource c = new();
                    _ = Task.Factory.StartNew(() =>
                    {
                        Console.ReadKey();
                        c.Cancel();
                    });



                    using (AlarmSubscription subscription = client.CreateAlarmSubscription())
                    {
                        System.Collections.Generic.IEnumerable<IPlcAlarm> pendingResults = await client.ReadPendingAlarmsAsync();
                        foreach (IPlcAlarm alarm in pendingResults)
                        {
                            Console.WriteLine($"Pending Alarm: ID: {alarm.Id}   MsgNumber: {alarm.MsgNumber} Id: {alarm.Id} IsAck: {alarm.IsAck} IsComing: {alarm.IsComing} IsGoing: {alarm.IsGoing} State: {alarm.State} EventState: {alarm.EventState} AckStateComing: {alarm.AckStateComing}  AckStateGoing: {alarm.AckStateGoing} ", alarm);
                        }
                        while (true)
                        {
                            AlarmUpdateResult results = await subscription.ReceiveAlarmUpdatesAsync(c.Token);
                            if (results.HasAlarms)
                            {
                                foreach (IPlcAlarm alarm in results.Alarms)
                                {
                                    Console.WriteLine($"Alarm update: ID: {alarm.Id}   MsgNumber: {alarm.MsgNumber} Id: {alarm.Id} IsAck: {alarm.IsAck} IsComing: {alarm.IsComing} IsGoing: {alarm.IsGoing} State: {alarm.State} EventState: {alarm.EventState} AckStateComing: {alarm.AckStateComing}  AckStateGoing: {alarm.AckStateGoing} ", alarm);
                                }
                            }
                            else if (!results.ChannelClosed)
                            {
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }


                    sw.Stop();
                    msTotal += sw.ElapsedMilliseconds;
                    logger?.LogDebug($"ReadAlarmsTime: {sw.Elapsed}");

                }
                catch (Exception ex)
                {
                    logger?.LogError($"Exception in read alarms {ex.Message}.");
                }

            }
            catch (Exception ex)
            {
                logger?.LogError($"An error occured in ReadAlarms: {ex.Message} - {ex.InnerException?.Message}");
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
