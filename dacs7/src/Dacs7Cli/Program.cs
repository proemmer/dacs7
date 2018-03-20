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
                long msTotal = 0;
                await client.ConnectAsync(readOptions.ConnectionString);
                var tags = ReadOperationParametersFromTags(readOptions.Tags);

                for (int i = 0; i < readOptions.Loops; i++)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var results = await client.ReadAnyAsync(tags);
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
            return tags.Select(t => ConvertFromNodeId(t) );
        }

        private static IEnumerable<WriteOperationParameter> WriteOperationParametersFromTags(IEnumerable<KeyValuePair<string, object>> tags)
        {
            return new List<WriteOperationParameter>();
        }

        public static ReadOperationParameter ConvertFromNodeId(string nodeId)
        {
            var parts = nodeId.Split(',');
            var startParts = parts[0].Split('.');
            var hasPrefix = startParts.Length == 3;
            var array = parts.Length == 3;
            var type = parts[1];
            PlcArea area = 0;
            Type readType = typeof(object);
            int datablock = -1;
            int numberOfItems = array ? Int32.Parse(parts[2]) : -1;

            switch (startParts[hasPrefix ? 1 : 0])
            {
                case "I": area = PlcArea.IB; break;
                case "M": area = PlcArea.FB; break;
                case "A": area = PlcArea.QB; break;
                case "T": area = PlcArea.TM; break;
                case "C": area = PlcArea.CT; break;
                case var s when Regex.IsMatch(s, "^DB\\d+$"):
                    {
                        area = PlcArea.DB;
                        datablock = Int32.Parse(s.Substring(2));
                        break;
                    }
            }

            var offset = Int32.Parse(startParts[hasPrefix ? 2 : 1]);

            switch (type.ToLower())
            {
                case "b": readType = typeof(byte); break;
                case "c": readType = typeof(char); break;
                case "w": readType = typeof(UInt16); break;
                case "dw": readType = typeof(UInt32); break;
                case "i": readType = typeof(Int16); break;
                case "di": readType = typeof(Int32); break;
                case "r": readType = typeof(Single); break;
                case "s": readType = typeof(String); break;
                case var s when Regex.IsMatch(s, "^x\\d+$"): readType = typeof(bool); break;
            }


            return new ReadOperationParameter
            {
                Area = area,
                Offset = offset,
                Type = readType,
                Args = datablock > 0 ? new[] { numberOfItems, datablock } : new [] { numberOfItems }
            };
        }
    }
}
