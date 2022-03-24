using Dacs7;
using Dacs7.DataProvider;
using Dacs7Cli.Options;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
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

                CommandOption portOption = cmd.Option("-p | --port", "The port of the plc", CommandOptionType.SingleValue);
                CommandOption debugOption = cmd.Option("-d | --debug", "Activate debug output", CommandOptionType.NoValue);
                CommandOption traceOption = cmd.Option("-t | --trace", "Trace also dacs7 internals", CommandOptionType.NoValue);
                CommandOption maxJobsOption = cmd.Option("-j | --jobs", "Maximum number of concurrent jobs.", CommandOptionType.SingleValue);
                CommandOption dataareas = cmd.Option("-t | --tags", "Tags used to register blocks for the simulation Provider (e.g. DB1.0,B,1000).", CommandOptionType.MultipleValue);
                CommandOption addressOption = cmd.Option("-a | --address", "The IPAddress of the plc is used by the Relay provider to connect to a plc.", CommandOptionType.SingleValue);
                CommandOption dataProvider = cmd.Option("-x | --provider", "Used data provider  (Simulation, Relay).", CommandOptionType.SingleValue);
                CommandOption pduSize = cmd.Option("-s | --pdu", "MaxPdu Size.", CommandOptionType.SingleValue);

                CommandArgument tagsArguments = cmd.Argument("tags", "Tags to read.", true);

                cmd.OnExecute(async () =>
                {
                    ServerOptions options = null; ;
                    try
                    {
                        options = new ServerOptions
                        {
                            Debug = debugOption.HasValue(),
                            Trace = traceOption.HasValue(),
                            Address = addressOption.HasValue() ? addressOption.Value() : "127.0.0.1:102",
                            MaxJobs = maxJobsOption.HasValue() ? int.Parse(maxJobsOption.Value()) : 10,
                            Port = portOption.HasValue() ? int.Parse(portOption.Value()) : 102,
                            Tags = dataareas.HasValue() ? dataareas.Values : null,
                            DataProvider = dataProvider.HasValue() ? dataProvider.Value() : null,
                            MaxPduSize = pduSize.HasValue() ? ushort.Parse(pduSize.Value()) : (ushort)960
                        }.Configure();
                        int result = await Serve(options, options.LoggerFactory);

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
            Dacs7Client client = null;
            IPlcDataProvider provider = null;
            if (string.IsNullOrWhiteSpace(options.DataProvider) || options.DataProvider.Equals("Simulation", StringComparison.InvariantCultureIgnoreCase))
            {
                if (options.Tags != null)
                {
                    foreach (string item in options.Tags)
                    {
                        ReadItem ri = ReadItem.CreateFromTag(item);
                        Console.WriteLine($"Register tag {item}");

                        SimulationPlcDataProvider.Instance.Register(ri.Area, ri.NumberOfItems, ri.DbNumber);
                    }
                }
                Console.WriteLine("Using Simulation Provider!");
                provider = SimulationPlcDataProvider.Instance;
            }
            else if (options.DataProvider.Equals("Relay", StringComparison.InvariantCultureIgnoreCase))
            {
                client = new Dacs7Client(options.Address, PlcConnectionType.Pg, 5000, loggerFactory)
                {
                    MaxAmQCalled = (ushort)options.MaxJobs,
                    MaxAmQCalling = (ushort)options.MaxJobs,
                    PduSize = options.MaxPduSize
                };
                RelayPlcDataProvider.Instance.UseClient(client);
                Console.WriteLine("Using Relay Provider!");
                provider = RelayPlcDataProvider.Instance;
            }

            Dacs7Server server = new(options.Port, provider, loggerFactory)
            {
                MaxAmQCalled = (ushort)options.MaxJobs,
                MaxAmQCalling = (ushort)options.MaxJobs,
                PduSize = 480
            };
            ILogger logger = loggerFactory?.CreateLogger("Dacs7Cli.Serve");

            try
            {

                Console.WriteLine($"Started serving on port {options.Port} !");
                await server.ConnectAsync();

                if (client != null)
                {
                    await client.ConnectAsync();
                }

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
                if (client != null)
                {
                    await client.DisconnectAsync();
                }
                await server.DisconnectAsync();
            }

            return 0;
        }

    }
}
