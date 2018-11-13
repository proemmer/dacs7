using Dacs7Cli.Options;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;

namespace Dacs7Cli
{



    class Program
    {


        public static int Main(string[] args)
        {
            try
            {
                var app = new CommandLineApplication
                {
                    FullName = "DacS7Cli",
                    Description = "DacS7 Commandline Interface"
                };

                ReadCommand.Register(app);
                WriteCommand.Register(app);
                BlockinfoCommand.Register(app);

                app.Command("help", cmd =>
                {
                    cmd.Description = "Get help for the application, or a specific command";

                    var commandArgument = cmd.Argument("<COMMAND>", "The command to get help for");
                    cmd.OnExecute(() =>
                    {
                        app.ShowHelp(commandArgument.Value);
                        return 0;
                    });
                });


                app.OnExecute(() =>
                {
                    app.ShowHelp();
                    return 0;
                });

                return app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured: {ex.Message}");
                return 1;
            }

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




    }
}
