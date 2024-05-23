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
        private static string _graphid = string.Empty;

        public static async Task Main(string[] args)
        {
            // Loads and configures
            var config = BuildConfig();
            var hivemqconfig = new HiveMQConfig();
            config.GetSection("HiveMQ").Bind(hivemqconfig);
            var dcrconfig = new DCRconfig();
            config.GetSection("DCR").Bind(dcrconfig);

            _graphid = AnsiConsole.Ask<string>("Graphid please: ");
            var tasklist = new List<Task>();

            using var client = new MqDcrService(hivemqconfig.Username, hivemqconfig.Password, hivemqconfig.Server,
                            hivemqconfig.Port, dcrconfig.Username, dcrconfig.Password);
            await client.ConnectAsync();
            await client.SetUpSubscriptions();

            var startsimresponse = await client.StartSimulation(_graphid);
            var dontTerminate = true;
            while (dontTerminate)
            {
                var enabledeventsresponse = await client.GetEnabledEvents(_graphid, startsimresponse.Simid);
                var dcrevent = SelectEvent(enabledeventsresponse.DCRevents.Where(p => p.Enabled).ToArray());
                var value = AnsiConsole.Ask<int>("Value please: ");
                await client.ExecuteEvent(_graphid, startsimresponse.Simid, dcrevent.EventID, value);
                await RunLazyUser(client, startsimresponse.Simid);
                var log = await client.GetLog(_graphid, startsimresponse.Simid);
                if (log.Any(p => p.EventId == "KYC_ACTIVITY")) dontTerminate = false;
            }
            await client.Terminate(_graphid, startsimresponse.Simid);

            // for (var j = 0; j < 1; j++)
            // {
            //     var t = Task.Run(async () =>
            //     {
            //         using var client = new MqDcrService(hivemqconfig.Username, hivemqconfig.Password, hivemqconfig.Server,
            //                 hivemqconfig.Port, dcrconfig.Username, dcrconfig.Password);
            //         await client.ConnectAsync();
            //         await client.SetUpSubscriptions();

            //         var startsimresponse = await client.StartSimulation(_graphid);
            //         for (var i = 0; i < 1; i++)
            //         {
            //             var enabledeventsresponse = await client.GetEnabledEvents(_graphid, startsimresponse.Simid);
            //             // var dcrevent = RandomEvent.GetRandomEvent(enabledeventsresponse.DCRevents);
            //             var dcrevent = enabledeventsresponse.DCRevents.First(p => p.EventID=="pengeoverfoersel");
            //             AnsiConsole.WriteLine($"Event: {dcrevent.Label} & ClientID: {client.ClientID()}");
            //             await client.ExecuteEvent(_graphid, startsimresponse.Simid, dcrevent.EventID, 30000);
            //             enabledeventsresponse = await client.GetEnabledEvents(_graphid, startsimresponse.Simid);
            //             foreach (var item in enabledeventsresponse.DCRevents.Where(p => p.Pending && p.Enabled)) {
            //                await client.ExecuteEvent(_graphid, startsimresponse.Simid, item.EventID, 30000);
            //             }
            //         }
            //     var log = await client.GetLog(_graphid, startsimresponse.Simid);
            //     await client.Terminate(_graphid,startsimresponse.Simid);
            //     });
            //     tasklist.Add(t);
            // }
            // await Task.WhenAll(tasklist.ToArray()).ConfigureAwait(false);
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

        private static async Task RunLazyUser(MqDcrService client, string simid)
        {
            var flag = true;
            while (flag)
            {
                flag = false;
                var enabledeventsresponse = await client.GetEnabledEvents(_graphid, simid);
                foreach (var item in enabledeventsresponse.DCRevents.Where(p => p.Pending && p.Enabled))
                {
                    await client.ExecuteEvent(_graphid, simid, item.EventID, 0);
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