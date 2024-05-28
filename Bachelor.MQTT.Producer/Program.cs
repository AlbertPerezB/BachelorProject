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
                new SpectreSelectionOption("BSc Albert Good Customer", "1822881"),
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

            for (var j = 0; j < 1; j++)
            {
                var t = Task.Run(async () =>
                {
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
                        // var dcrevent = enabledeventsresponse.DCRevents.First(p => p.EventID == "pengeoverfoersel");
                        var valuedict = await client.ExecuteValueEvent(customergraphid, startcustomerresponse.Simid, dcrevent.EventID, "0");
                        AnsiConsole.WriteLine($"Event: {dcrevent.Label} with value: {valuedict[dcrevent.EventID]} & ClientID: {client.ClientID()}");
                        var enableddetectorresponse = await client.GetEnabledEvents(detectorgraphid, startdetectorresponse.Simid);
                        var detectoreventid = dcrevent.EventID;
                        dcrevent = enableddetectorresponse.DCRevents.First(p => p.Label == dcrevent.Label);
                        await client.ExecuteValueEvent(detectorgraphid, startdetectorresponse.Simid, dcrevent.EventID, valuedict[detectoreventid]); // don't need to store result
                        await RunLazyUser(client, detectorgraphid, startdetectorresponse.Simid);
                        var log = await client.GetLog(detectorgraphid, startdetectorresponse.Simid);
                        if (log.Any(p => p.EventId == "KYC_ACTIVITY"))
                        {
                            // Thread.Sleep(2500);
                            await client.Terminate(detectorgraphid, startdetectorresponse.Simid);
                            await client.Terminate(customergraphid, startcustomerresponse.Simid);
                            // AnsiConsole.Clear();
                            ShowFlashingMessage("SUS DETECTED!", 8);
                            Console.ReadLine();
                            break;
                        }

                    }
                });
                tasklist.Add(t);
            }
            await Task.WhenAll(tasklist.ToArray()).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets ud the configuration, adding the appsettings file and environment variables
        /// </summary>
        /// <returns></returns> <summary>
        /// The IConfigurationRoot which the builder returns
        /// </summary>
        /// <returns></returns>
        private static IConfigurationRoot BuildConfig()
        {
            var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, false)
            .AddEnvironmentVariables();
            return builder.Build();
        }


        static void ShowFlashingMessage(string message, int durationInSeconds)
        {
            var figletText = new FigletText(message)
                .Centered()
                .Color(Color.Red);
            // Render the message
            AnsiConsole.Write(figletText);

            // Wait for the specified duration
            Thread.Sleep(durationInSeconds * 1000);
        }
        private static async Task RunLazyUser(MqDcrService client, string graphid, string simid)
        {
            var flag = true;
            while (flag)
            {
                flag = false;
                var enabledeventsresponse = await client.GetEnabledEvents(graphid, simid);
                foreach (var item in enabledeventsresponse.DCRevents.Where(p => p.Pending && p.Enabled))
                {
                    await client.ExecuteEvent(graphid, simid, item.EventID);
                    AnsiConsole.WriteLine($"Event: {item.Label} & ClientID: {client.ClientID()} (executed by Lazy User)");
                    flag = true;
                }
            }
        }

        private static DCRevent SelectEvent(DCRevent[] events)
        {
            return AnsiConsole.Prompt(
            new SelectionPrompt<DCRevent>()
                .Title("Select an event")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more events)[/]")
                .UseConverter(p => p.Label)
                .AddChoices(events));
        }
    }
}