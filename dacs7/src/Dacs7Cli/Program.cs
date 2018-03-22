using CommandLine;
using Dacs7;
using Dacs7Cli.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dacs7Cli
{

    class Program
    {
        private static ILoggerFactory _factory;
        private static ILogger _logger;

        static int Main(string[] args)
        {
            try
            {
                var options = Parser.Default.ParseArguments<ReadOptions, WriteOptions/*, WatchOptions, WatchAlarmsOptions*/>(args);
                if (options.Tag == ParserResultType.Parsed)
                {
                    try
                    {
                        return options.MapResult(
                            (ReadOptions o) => Read(Configure(o)).ConfigureAwait(false).GetAwaiter().GetResult(),
                            (WriteOptions o) => Write(Configure(o)).ConfigureAwait(false).GetAwaiter().GetResult(),
                            //(WatchOptions o) => Watch(Configure(o)).ConfigureAwait(false).GetAwaiter().GetResult(),
                            //(WatchAlarmsOptions o) => WatchAlarms(Configure(o)).ConfigureAwait(false).GetAwaiter().GetResult(),
                            _ => 1);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"An error occured: {ex.Message}");
                        return 1;
                    }
                    finally
                    {
                        _factory?.Dispose();
                    }

                }
            }
            catch(Exception ex)
            {

            }
            return 1;
        }



        //private static async Task<int> Watch(WatchOptions watchOptions)
        //{
        //    var client = new Dacs7Client(_factory);
        //    try
        //    {
        //        await client.ConnectAsync(watchOptions.ConnectionString);

        //        client.Subscribe(1000, (Subscription subscription, DataChangeNotification notification, IList<string> stringTable) =>
        //        {
        //            foreach (var item in notification.MonitoredItems)
        //            {
        //                var clientItem = subscription.FindItemByClientHandle(item.ClientHandle);
        //                _logger.LogInformation($"DataChanged: {clientItem.DisplayName}={item.Value}");
        //            }

        //        }, watchOptions.Tags.Select(x => MonitorItem.Create(x, 1)));

        //        Console.WriteLine("Press any key to stop!");
        //        Console.ReadKey();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"An error occured in Watch: {ex.Message}");
        //        return 1;
        //    }
        //    finally
        //    {
        //        await client.DisconnectAsync();
        //    }

        //    return 0;
        //}

        //private static async Task<int> WatchAlarms(WatchAlarmsOptions watchOptions)
        //{
        //    var client = new Dacs7Client(_factory);
        //    try
        //    {
        //        await client.ConnectAsync(watchOptions.ConnectionString);

        //        var filter = new EventFilter
        //        {
        //            SelectClauses = Events.DefaultEventAttributes
        //        };
        //        filter.CreateDefaultFilter(1, 1000, null);

        //        await client.Subscribe(100, (Subscription subscription, EventNotificationList notification, IList<string> stringTable) =>
        //        {
        //            foreach (var item in notification.Events)
        //            {
        //                _logger.LogInformation($"Event: {item.EventFields.Aggregate((x, y) => $"{x.ToString()};{y.ToString()}") }");
        //            }

        //        }, new[] { MonitorItem.Create(Objects.Server, 100, filter, Attributes.EventNotifier) })
        //        .Refresh();


        //        Console.WriteLine("Press any key to stop!");
        //        Console.ReadKey();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"An error occured in Watch: {ex.Message}");
        //        return 1;
        //    }
        //    finally
        //    {
        //        await client.DisconnectAsync();
        //    }

        //    return 0;
        //}


        private static async Task<int> Write(WriteOptions writeOptions)
        {
            var client = new Dacs7Client(writeOptions.Address);
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
                    _logger.LogInformation($"Write: {item.Key}={item.Value}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occured in Write: {ex.Message}");
                return 1;
            }
            finally
            {
                await client.DisconnectAsync();
            }

            return 0;
        }

        private static async Task<int> Read(ReadOptions readOptions)
        {
            var client = new Dacs7Client(readOptions.Address);
            try
            {
                long msTotal = 0;
                await client.ConnectAsync();

                if (readOptions.RegisterItems)
                {
                    await client.RegisterAsync(readOptions.Tags);
                }

                for (int i = 0; i < readOptions.Loops; i++)
                {
                    if(i > 0 && readOptions.Wait > 0)
                    {
                        await Task.Delay(readOptions.Wait);
                    }

                    var sw = new Stopwatch();
                    sw.Start();
                    var results = await client.ReadAsync(readOptions.Tags);
                    sw.Stop();
                    msTotal += sw.ElapsedMilliseconds;
                    _logger.LogDebug($"ReadTime: {sw.Elapsed}");

                    var resultEnumerator = results.GetEnumerator();
                    foreach (var item in readOptions.Tags)
                    {
                        if (resultEnumerator.MoveNext())
                        {
                            _logger.LogInformation($"Read: {item}={resultEnumerator.Current}");
                        }
                    }
                }

                if(readOptions.Loops > 0)
                    _logger?.LogInformation($"Average read time over loops is {msTotal / readOptions.Loops}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occured in Read: {ex.Message}");
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

        private static T Configure<T>(T options) where T : OptionsBase
        {
            _factory = new LoggerFactory().AddConsole(options.Debug ? LogLevel.Debug : LogLevel.Information);
            _logger = _factory.CreateLogger<Program>();
            return options;
        }


    }
}
