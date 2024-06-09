using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Bachelor.MQTT.Shared;
using Bachelor.MQTT.Shared.MessageModels;
using HiveMQtt.Client;
using HiveMQtt.Client.Events;
using HiveMQtt.Client.Options;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Configuration;
using Spectre.Console;


namespace Bachelor.MQTT.Producer
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // Loads and configures
            var config = BuildConfig();
            var hivemqconfig = new HiveMQConfig();
            config.GetSection("HiveMQ").Bind(hivemqconfig);
            var dcrconfig = new DCRconfig();
            config.GetSection("DCR").Bind(dcrconfig);

            AnsiConsole.Write(
                new FigletText("DCR with HiveMQ simulator")
                    .LeftJustified()
                    .Color(Color.Blue));

            var detectorgraphid = AnsiConsole.Prompt(new TextPrompt<string>("Detector graph-id please [cyan](default: BSc Albert Detector)[/]: ")
                .DefaultValue("1822861").PromptStyle("cyan").HideDefaultValue());

            var options = new[]
            {
                new SpectreSelectionOption("BSc Albert Sus Customer", "1822880"),
                new SpectreSelectionOption("BSc Albert Good Customer", "1823571"),
                new SpectreSelectionOption("Write your own...", "")
            };

            var customergraphid = Convert.ToString(AnsiConsole.Prompt(
                new SelectionPrompt<SpectreSelectionOption>().Title("Choose a customer behaviour graph or enter your own graph-id:")
                    .AddChoices(options)).Value)!;

            if (customergraphid == "") customergraphid = AnsiConsole.Prompt(new TextPrompt<string>("Enter graph-id: "));

            AnsiConsole.Clear();

            var tasklist = new List<Task>();

            using var client = new MqDcrService(hivemqconfig.Username, hivemqconfig.Password, hivemqconfig.Server,
                            hivemqconfig.Port, dcrconfig.Username, dcrconfig.Password);
            await client.ConnectAsync();
            await client.SetUpSubscriptions();

            var table = new Table().Centered()
                .AddColumn(new TableColumn("Client1"))
                .AddColumn("Client2")
                .AddColumn("Client3").Expand();

            table.Title = new TableTitle("[blue]HiveMQ simulator[/] - [yellow4]Lazy User[/]", Style.Parse("bold"));

            await AnsiConsole.Live(table)
                .AutoClear(false)   // Do not remove when done
                .Overflow(VerticalOverflow.Visible) // Show ellipsis when overflowing
                .StartAsync(async ctx =>
                {
                    await RunClients(hivemqconfig, dcrconfig, detectorgraphid, customergraphid, tasklist, table, ctx).ConfigureAwait(false);
                    ctx.Refresh();
                    Console.ReadLine();
                });
        }

        /// <summary>
        /// Function that creates 3 clients, starts a simulation on each and waits for them all to finish
        /// </summary>
        /// <param name="hivemqconfig"></param>
        /// <param name="dcrconfig"></param>
        /// <param name="detectorgraphid"></param>
        /// <param name="customergraphid"></param>
        /// <param name="tasklist"></param>
        /// <param name="table"></param>
        /// <param name="ctx"></param>
        private static async Task RunClients(HiveMQConfig hivemqconfig, DCRconfig dcrconfig, string detectorgraphid, string customergraphid, List<Task> tasklist, Table table, LiveDisplayContext ctx)
        {
            Task t1 = CreateClient(hivemqconfig, dcrconfig, detectorgraphid, customergraphid, table, ctx, 0);
            Task t2 = CreateClient(hivemqconfig, dcrconfig, detectorgraphid, customergraphid, table, ctx, 1);
            Task t3 = CreateClient(hivemqconfig, dcrconfig, detectorgraphid, customergraphid, table, ctx, 2);

            await Task.WhenAll(t1, t2, t3).ConfigureAwait(false);
            table.Caption("All producers done");
        }

        /// <summary>
        /// Runs a hivemq producer client
        /// </summary>
        /// <param name="hivemqconfig"></param>
        /// <param name="dcrconfig"></param>
        /// <param name="detectorgraphid"></param>
        /// <param name="customergraphid"></param>
        /// <param name="table"></param>
        /// <param name="ctx"></param>
        /// <param name="number"></param>
        private static Task CreateClient(HiveMQConfig hivemqconfig, DCRconfig dcrconfig, string detectorgraphid, string customergraphid, Table table, LiveDisplayContext ctx, int number)
        {
            return Task.Run(async () =>
            {
                await ExecuteSimulation(hivemqconfig, dcrconfig, detectorgraphid, customergraphid, table, ctx, number);
            });
        }

        /// <summary>
        /// Connects client to broker and sets up subscriptions. Then runs the simulation.
        /// </summary>
        /// <param name="hivemqconfig"></param>
        /// <param name="dcrconfig"></param>
        /// <param name="detectorgraphid"></param>
        /// <param name="customergraphid"></param>
        /// <param name="table"></param>
        /// <param name="ctx"></param>
        /// <param name="j"></param>
        private static async Task ExecuteSimulation(HiveMQConfig hivemqconfig, DCRconfig dcrconfig, string detectorgraphid, string customergraphid, Table table, LiveDisplayContext ctx, int j)
        {
            var susflag = false;
            using var client = new MqDcrService(hivemqconfig.Username, hivemqconfig.Password, hivemqconfig.Server,
                                        hivemqconfig.Port, dcrconfig.Username, dcrconfig.Password);
            await client.ConnectAsync();
            await client.SetUpSubscriptions();

            var startdetectorresponse = await client.StartSimulation(detectorgraphid);
            var startcustomerresponse = await client.StartSimulation(customergraphid);

            for (var i = 0; i < 5; i++)
            {
                var enabledcustomerresponse = await client.GetEnabledEvents(customergraphid, startcustomerresponse.Simid);
                var dcrevent = RandomEvent.GetRandomEvent(enabledcustomerresponse.DCRevents);
                var valuedict = await client.ExecuteValueEvent(customergraphid, startcustomerresponse.Simid, dcrevent.EventID, "0");
                var enableddetectorresponse = await client.GetEnabledEvents(detectorgraphid, startdetectorresponse.Simid);
                var detectoreventid = dcrevent.EventID;
                dcrevent = enableddetectorresponse.DCRevents.First(p => p.Label == dcrevent.Label);
                try
                {
                    AddRow(table, j, dcrevent.Label + ": " + valuedict[detectoreventid]);
                    await client.ExecuteValueEvent(detectorgraphid, startdetectorresponse.Simid, dcrevent.EventID, valuedict[detectoreventid]); // don't need to store result
                }
                catch (KeyNotFoundException)
                {
                    AddRow(table, j, dcrevent.Label);
                    await client.ExecuteValueEvent(detectorgraphid, startdetectorresponse.Simid, dcrevent.EventID, "0"); // don't need to store result
                }
                if (i != 4) ctx.Refresh(); // If-statement needed so it renders final table correctly
                await RunLazyUser(client, detectorgraphid, startdetectorresponse.Simid, table, j);
                var log = await client.GetLog(detectorgraphid, startdetectorresponse.Simid);
                if (log.Any(p => p.EventId == "KYC_ACTIVITY"))
                {
                    await client.Terminate(detectorgraphid, startdetectorresponse.Simid);
                    await client.Terminate(customergraphid, startcustomerresponse.Simid);
                    susflag = true;
                    break;
                }
            }
            if (susflag) AddRow(table, j, "[red]SUS DETECTED!!![/]");
            else AddRow(table, j, "[green]Finished with no sus![/]");
        }

        /// <summary>
        /// Add a row to the Spectre table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="j"></param>
        /// <param name="message"></param>
        private static void AddRow(Table table, int j, string message)
        {
            table.AddRow(j == 0 ? message : string.Empty,
                                j == 1 ? message : string.Empty,
                                j == 2 ? message : string.Empty);
        }

        /// <summary>
        /// Sets ud the configuration, adding the appsettings file and environment variables
        /// <returns> The IConfigurationRoot which the builder returns </returns>
        private static IConfigurationRoot BuildConfig()
        {
            var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, false)
            .AddEnvironmentVariables();
            return builder.Build();
        }
        /// <summary>
        /// Exectues all pending events and adds row in table with yellow text.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="graphid"></param>
        /// <param name="simid"></param>
        /// <param name="table"></param>
        /// <param name="j"></param>
        private static async Task RunLazyUser(MqDcrService client, string graphid, string simid, Table table, int j)
        {
            var flag = true;
            while (flag)
            {
                flag = false;
                var enabledeventsresponse = await client.GetEnabledEvents(graphid, simid);
                foreach (var item in enabledeventsresponse.DCRevents.Where(p => p.Pending && p.Enabled))
                {
                    await client.ExecuteEvent(graphid, simid, item.EventID);
                    AddRow(table, j, $"[yellow4]{item.Label}[/]");
                    flag = true;
                }
            }
        }
    }
}