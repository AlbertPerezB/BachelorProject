using Microsoft.Extensions.Options;
using HiveMQtt.Client;
using HiveMQtt.Client.Events;
using System.Text.Json;
using Bachelor.MQTT.Shared.MessageModels;
using HiveMQtt.MQTT5.Types;
using Bachelor.MQTT.Shared;

namespace Bachelor.MQTT.Subscriber;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HiveMQConfig _config;
    private readonly DCRservice _dcrservice;

    public Worker(ILogger<Worker> logger, IOptions<HiveMQConfig> config, DCRservice dcrservice)
    {
        _logger = logger;
        _config = config.Value;
        _dcrservice = dcrservice;
    }

    /// <summary>
    /// Disconnects (shuts down) client
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    private async Task ShutDown(HiveMQClient client)
    {
        if (client == null) return;
        try
        {
            await client.DisconnectAsync();
            client.Dispose();
        }
        catch (Exception)
        {

        }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = await Subscribe();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(100, stoppingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error ocurred");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ocurred");
        }
        finally
        {
            // await ShutDown(client);
        }

    }

    /// <summary>
    /// Connects client and subscribes to all the graph operations 
    /// </summary>
    /// <returns>Client</returns>
    private async Task<HiveMQClient> Subscribe()
    {
        var options = new HiveMQClientOptionsBuilder()
            .WithBroker(_config.Server)
            .WithPort(_config.Port)
            .WithUserName(_config.Username)
            .WithPassword(_config.Password)
            .WithUseTls(true)
            .Build();

        var client = new HiveMQClient(options);
        client.OnMessageReceived += async (s, e) =>
        {
            _logger.LogInformation($"Received: {e.PublishMessage.Topic} with responsetopic: {e.PublishMessage.ResponseTopic}");
            switch (e.PublishMessage.Topic)
            {
                case var p when p == "DCR/StartSimulation":
                    await StartSimulation(e, client);
                    break;
                case var p when p == "DCR/GetEnabledEvents":
                    await GetEnabledEvents(e, client);
                    break;
                case var p when p == "DCR/ExecuteEvent":
                    await ExecuteEvent(e, client);
                    break;
                case var p when p == "DCR/Terminate":
                    await Terminate(e, client);
                    break;
            }
        };
        var builder = new SubscribeOptionsBuilder();
        builder.WithSubscription("$share/dcrgroup/DCR/StartSimulation", QualityOfService.AtLeastOnceDelivery)
        .WithSubscription("$share/dcrgroup/DCR/GetEnabledEvents", QualityOfService.AtLeastOnceDelivery)
        .WithSubscription("$share/dcrgroup/DCR/ExecuteEvent", QualityOfService.AtLeastOnceDelivery)
        .WithSubscription("$share/dcrgroup/DCR/Terminate", QualityOfService.AtLeastOnceDelivery);
        var subscribeoptions = builder.Build();

        await client.ConnectAsync();
        await client.SubscribeAsync(subscribeoptions);
        return client;
    }

    /// <summary>
    /// Calls the StartSimulation from dcrservice and publishes the response message
    /// </summary>
    /// <param name="e"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    private async Task StartSimulation(OnMessageReceivedEventArgs e, HiveMQClient client)
    {
        var request = JsonSerializer.Deserialize<StartSimulationRequest>(e.PublishMessage.PayloadAsString);
        var simid = await _dcrservice.StartSimulation(request!.Graphid, GetUsername(e.PublishMessage), GetPassword(e.PublishMessage));
        var response = new StartSimulationResponse() { Simid = simid };
        var msg = new MQTT5PublishMessage(e.PublishMessage.ResponseTopic!, QualityOfService.ExactlyOnceDelivery);
        msg.CorrelationData = e.PublishMessage.CorrelationData;
        msg.PayloadAsString = JsonSerializer.Serialize(response);
        await client.PublishAsync(msg);
    }

    /// <summary>
    /// Calls the GetEnabledEvents from dcrservice and publishes the response message
    /// </summary>
    /// <param name="e"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    private async Task GetEnabledEvents(OnMessageReceivedEventArgs e, HiveMQClient client)
    {
        var request = JsonSerializer.Deserialize<GetEnabledEventsRequest>(e.PublishMessage.PayloadAsString);
        var events = await _dcrservice.GetEnabledEvent(request!.Graphid, request.Simid, GetUsername(e.PublishMessage), GetPassword(e.PublishMessage));
        var response = new GetEnabledEventsResponse()
        {
            IsAccepting = events.IsAccepting == "True",
            DCRevents = events.Event.Select(p => new DCRevent
            {
                Description = p.Description,
                EventID = p.Id,
                Label = p.Label
            })
                .ToArray()
        };

        var msg = new MQTT5PublishMessage(e.PublishMessage.ResponseTopic!, QualityOfService.ExactlyOnceDelivery);
        msg.CorrelationData = e.PublishMessage.CorrelationData;
        msg.PayloadAsString = JsonSerializer.Serialize(response);
        await client.PublishAsync(msg);
    }

    /// <summary>
    /// Calls the ExecuteEvent from dcrservice and publishes the response message (string.Empty)
    /// </summary>
    /// <param name="e"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    private async Task ExecuteEvent(OnMessageReceivedEventArgs e, HiveMQClient client)
    {
        var request = JsonSerializer.Deserialize<ExecuteEventRequest>(e.PublishMessage.PayloadAsString);
        await _dcrservice.ExecuteEvent(request!.Graphid, request.Simid, request.EventID, GetUsername(e.PublishMessage), GetPassword(e.PublishMessage));

        var msg = new MQTT5PublishMessage(e.PublishMessage.ResponseTopic!, QualityOfService.ExactlyOnceDelivery);
        msg.CorrelationData = e.PublishMessage.CorrelationData;
        msg.PayloadAsString = string.Empty;
        await client.PublishAsync(msg);
    }

    /// <summary>
    /// Calls the Terminate from dcrservice and publishes the response message (string.Empty)
    /// </summary>
    /// <param name="e"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    private async Task Terminate(OnMessageReceivedEventArgs e, HiveMQClient client)
    {
        var request = JsonSerializer.Deserialize<TerminateRequest>(e.PublishMessage.PayloadAsString);
        await _dcrservice.Terminate(request!.Graphid, request.Simid, GetUsername(e.PublishMessage), GetPassword(e.PublishMessage));

        var msg = new MQTT5PublishMessage(e.PublishMessage.ResponseTopic!, QualityOfService.ExactlyOnceDelivery);
        msg.CorrelationData = e.PublishMessage.CorrelationData;
        msg.PayloadAsString = string.Empty;
        await client.PublishAsync(msg);
    }

    /// <summary>
    /// Gets the username from a message
    /// </summary>
    /// <param name="msg"></param>
    /// <returns>Username as string</returns>
    private string GetUsername(MQTT5PublishMessage msg)
    {
        return msg.UserProperties["Username"];
    }

    /// <summary>
    /// Gets the password from a message
    /// </summary>
    /// <param name="msg"></param>
    /// <returns>Username as string</returns>
    private string GetPassword(MQTT5PublishMessage msg)
    {
        return msg.UserProperties["Password"];
    }
}
