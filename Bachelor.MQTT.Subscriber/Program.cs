using System.Text;
using Bachelor.MQTT.Shared;
using Bachelor.MQTT.Subscriber;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<Worker>();
        services.Configure<HiveMQConfig>(context.Configuration.GetSection("HiveMQ"));
        services.AddHttpClient<DCRservice>(client =>
        {
            client.BaseAddress = new Uri("https://repository.dcrgraphs.net/");
        });
    })
    .Build();
host.Run();
