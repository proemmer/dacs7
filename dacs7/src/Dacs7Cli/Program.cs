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
            var client = new Dacs7Client(_factory);
            try
            {
                await client.ConnectAsync(writeOptions.ConnectionString);
                var write = writeOptions.Tags.Select(x =>
                {
                    var s = x.Split('=');
                    return KeyValuePair.Create<string, object>(s[0], s[1]);
                }
                ).ToList();

                await client.WriteAnyAsync(WriteOperationParametersFromTags(write));

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
            var client = new Dacs7Client(_factory);
            try
            {
                await client.ConnectAsync(readOptions.ConnectionString);


                for (int i = 0; i < readOptions.Loops; i++)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var results = await client.ReadAnyAsync(ReadOperationParametersFromTags(readOptions.Tags));
                    sw.Stop();
                    _logger.LogDebug($"Read: {sw.Elapsed}");

                    var resultEnumerator = results.GetEnumerator();
                    foreach (var item in readOptions.Tags)
                    {
                        if (resultEnumerator.MoveNext())
                        {
                            _logger.LogInformation($"Read: {item}={resultEnumerator.Current}");
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occured in Read: {ex.Message}");
                return 1;
            }
            finally
            {
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


        private static IEnumerable<ReadOperationParameter> ReadOperationParametersFromTags(IEnumerable<string> tags)
        {
            var methodName = typeof(ReadOperationParameter).GetMethod(nameof(ReadOperationParameter.Create));
            tags.Select(t =>
            {
                methodName.MakeGenericMethod(ConvertFromNodeId(t))
            });
            return new List<ReadOperationParameter>();
        }

        private static IEnumerable<WriteOperationParameter> WriteOperationParametersFromTags(IEnumerable<KeyValuePair<string, object>> tags)
        {
            return new List<WriteOperationParameter>();
        }

        public static Type ConvertFromNodeId(string nodeId)
        {
            var parts = nodeId.Split(',');
            var array = parts.Length == 3;
            var type = parts[1];

            switch (type.ToLower())
            {
                case "b": return array ? typeof(byte[])   : typeof(byte);
                case "c": return array ? typeof(char[])   : typeof(char);
                case "w": return array ? typeof(UInt16[]) : typeof(UInt16);
                case "dw": return array ?typeof(UInt32[]) : typeof(UInt32);
                case "i": return array ? typeof(Int16[])  : typeof(Int16);
                case "di": return array ?typeof(Int32[])  : typeof(Int32);
                case "r": return array ? typeof(Single[]) : typeof(Single);
                case "s": return array ? typeof(String[]) : typeof(String);
                case var s when Regex.IsMatch(s, "^x\\d+$"): return array ?typeof(bool[]) : typeof(bool);
            }
            return value;
        }
    }
}
