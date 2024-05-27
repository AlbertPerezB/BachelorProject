using System.Collections.Concurrent;
using System.Text.Json;
using Bachelor.MQTT.Shared;
using Bachelor.MQTT.Shared.MessageModels;
using HiveMQtt.Client;
using HiveMQtt.MQTT5.Types;

namespace Bachelor.MQTT.Producer;

public class MqDcrService : IDisposable
{
    private bool _disposed = false;

    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<string>> _responses;
    // Implement IDisposable.
    // Do not make this method virtual.
    // A derived class should not be able to override this method.
    private HiveMQClient _client;
    private readonly string _dcrusername;
    private readonly string _dcrpassword;
    public MqDcrService(string username, string password, string server, int port, string dcrusername, string dcrpassword)
    {
        var options = new HiveMQClientOptionsBuilder()
            .WithBroker(server)
            .WithPort(port)
            .WithUserName(username)
            .WithPassword(password)
            .WithUseTls(true)
            .Build();
        _dcrusername = dcrusername;
        _dcrpassword = dcrpassword;
        _client = new HiveMQClient(options);
        _responses = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
    }

    /// <summary>
    /// Connects the client
    /// </summary>
    public async Task ConnectAsync()
    {
        await _client.ConnectAsync();
    }

    /// <summary>
    /// Returns the client id 
    /// </summary>
    public string ClientID() => _client.Options.ClientId!;

    /// <summary>
    /// Subscribes to all the graph operations and sets the OnMessageReceived 
    /// </summary>
    public async Task SetUpSubscriptions()
    {
        var builder = new SubscribeOptionsBuilder();
        builder.WithSubscription("DCR/StartSimulation/" + _client.Options.ClientId, HiveMQtt.MQTT5.Types.QualityOfService.AtMostOnceDelivery)
        .WithSubscription("DCR/GetEnabledEvents/" + _client.Options.ClientId, HiveMQtt.MQTT5.Types.QualityOfService.AtMostOnceDelivery)
        .WithSubscription("DCR/ExecuteEvent/" + _client.Options.ClientId, HiveMQtt.MQTT5.Types.QualityOfService.AtMostOnceDelivery)
        .WithSubscription("DCR/ExecuteValueEvent/" + _client.Options.ClientId, HiveMQtt.MQTT5.Types.QualityOfService.AtMostOnceDelivery)
        .WithSubscription("DCR/Terminate/" + _client.Options.ClientId, HiveMQtt.MQTT5.Types.QualityOfService.AtMostOnceDelivery)
        .WithSubscription("DCR/GetLog/" + _client.Options.ClientId, HiveMQtt.MQTT5.Types.QualityOfService.AtMostOnceDelivery);
        var subscribeoptions = builder.Build();

        _client.OnMessageReceived += (s, e) =>
        {
            var key = new Guid(e.PublishMessage.CorrelationData!);
            if (_responses.TryRemove(key, out var tcs))
            {
                tcs.TrySetResult(e.PublishMessage.PayloadAsString);
            }
        };
        await _client.SubscribeAsync(subscribeoptions);
    }
    /// <summary>
    /// Starts of a new instance of a graph simulation.   
    /// </summary>
    /// <param name="graphid"></param>
    /// <returns>A response instance with the simID</returns>
    public async Task<StartSimulationResponse> StartSimulation(string graphid)
    {
        var msg = new MQTT5PublishMessage("DCR/StartSimulation", QualityOfService.AtMostOnceDelivery); // Can change qos later
        SetCredentials(msg);
        var key = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>();
        var c = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Time out after 30 seconds
        c.Token.Register(() =>
        {
            if (_responses.TryRemove(key, out var _))
            {
                tcs.TrySetCanceled();
            }
        }, false);
        _responses.TryAdd(key, tcs);
        msg.CorrelationData = key.ToByteArray();
        msg.ResponseTopic = "DCR/StartSimulation/" + _client.Options.ClientId;
        var request = new StartSimulationRequest { Graphid = graphid };
        msg.PayloadAsString = JsonSerializer.Serialize(request);
        await _client.PublishAsync(msg).ConfigureAwait(false);
        var result = await tcs.Task.ConfigureAwait(false);
        return JsonSerializer.Deserialize<StartSimulationResponse>(result)!;
    }

    /// <summary>
    /// Gets all the enabled events at the time of invocation 
    /// </summary>
    /// <param name="graphid"></param>
    /// <param name="simid"></param>
    /// <returns>Response instance with events, isaccepting and so on</returns>
    public async Task<GetEnabledEventsResponse> GetEnabledEvents(string graphid, string simid)
    {
        var msg = new MQTT5PublishMessage("DCR/GetEnabledEvents", QualityOfService.ExactlyOnceDelivery); // Can change qos later
        SetCredentials(msg);
        var key = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>();
        var c = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Time out after 30 seconds
        c.Token.Register(() =>
        {
            if (_responses.TryRemove(key, out var _))
            {
                tcs.TrySetCanceled();
            }
        }, false);
        _responses.TryAdd(key, tcs);
        msg.CorrelationData = key.ToByteArray();
        msg.ResponseTopic = "DCR/GetEnabledEvents/" + _client.Options.ClientId;
        var request = new GetEnabledEventsRequest { Graphid = graphid, Simid = simid };
        msg.PayloadAsString = JsonSerializer.Serialize(request);
        await _client.PublishAsync(msg).ConfigureAwait(false);
        var result = await tcs.Task;
        return JsonSerializer.Deserialize<GetEnabledEventsResponse>(result)!;
    }

    /// <summary>
    /// Executes an event, thus updating graph and enabled events  
    /// </summary>
    /// <param name="graphid"></param>
    /// <param name="simid"></param>
    /// <param name="eventid"></param>
    public async Task ExecuteEvent(string graphid, string simid, string eventid, int value)
    {
        var msg = new MQTT5PublishMessage("DCR/ExecuteEvent", QualityOfService.ExactlyOnceDelivery); // Can change qos later
        SetCredentials(msg);
        var key = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>();
        var c = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Time out after 30 seconds
        c.Token.Register(() =>
        {
            if (_responses.TryRemove(key, out var _))
            {
                tcs.TrySetCanceled();
            }
        }, false);
        _responses.TryAdd(key, tcs);
        msg.CorrelationData = key.ToByteArray();
        msg.ResponseTopic = "DCR/ExecuteEvent/" + _client.Options.ClientId;
        var request = new ExecuteEventRequest { Graphid = graphid, Simid = simid, EventID = eventid, Value = value };
        msg.PayloadAsString = JsonSerializer.Serialize(request);
        await _client.PublishAsync(msg).ConfigureAwait(false);
        await tcs.Task;
    }
    
    /// <summary>
    /// Executes an event, thus updating graph and enabled events  
    /// </summary>
    /// <param name="graphid"></param>
    /// <param name="simid"></param>
    /// <param name="eventid"></param>
    public async Task<Dictionary<string, string>> ExecuteValueEvent(string graphid, string simid, string eventid, int value)
    {
        var msg = new MQTT5PublishMessage("DCR/ExecuteValueEvent", QualityOfService.ExactlyOnceDelivery); // Can change qos later
        SetCredentials(msg);
        var key = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>();
        var c = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Time out after 30 seconds
        c.Token.Register(() =>
        {
            if (_responses.TryRemove(key, out var _))
            {
                tcs.TrySetCanceled();
            }
        }, false);
        _responses.TryAdd(key, tcs);
        msg.CorrelationData = key.ToByteArray();
        msg.ResponseTopic = "DCR/ExecuteValueEvent/" + _client.Options.ClientId;
        var request = new ExecuteEventRequest { Graphid = graphid, Simid = simid, EventID = eventid, Value = value };
        msg.PayloadAsString = JsonSerializer.Serialize(request);
        await _client.PublishAsync(msg).ConfigureAwait(false);
        var result = await tcs.Task;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(result)!;
    }

    /// <summary>
    /// Deletes an instance of a graph simulation.
    /// </summary>
    /// <param name="graphid"></param>
    /// <param name="simid"></param>
    public async Task Terminate(string graphid, string simid)
    {
        var msg = new MQTT5PublishMessage("DCR/Terminate", QualityOfService.ExactlyOnceDelivery); // Can change qos later
        SetCredentials(msg);
        var key = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>();
        var c = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Time out after 30 seconds
        c.Token.Register(() =>
        {
            if (_responses.TryRemove(key, out var _))
            {
                tcs.TrySetCanceled();
            }
        }, false);
        _responses.TryAdd(key, tcs);
        msg.CorrelationData = key.ToByteArray();
        msg.ResponseTopic = "DCR/Terminate/" + _client.Options.ClientId;
        var request = new GetEnabledEventsRequest { Graphid = graphid, Simid = simid };
        msg.PayloadAsString = JsonSerializer.Serialize(request);
        await _client.PublishAsync(msg).ConfigureAwait(false);
        await tcs.Task;
    }

    public async Task<LogEntry[]> GetLog(string graphid, string simid)
    {
        var msg = new MQTT5PublishMessage("DCR/GetLog", QualityOfService.ExactlyOnceDelivery); // Can change qos later
        SetCredentials(msg);
        var key = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>();
        var c = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Time out after 30 seconds
        c.Token.Register(() =>
        {
            if (_responses.TryRemove(key, out var _))
            {
                tcs.TrySetCanceled();
            }
        }, false);
        _responses.TryAdd(key, tcs);
        msg.CorrelationData = key.ToByteArray();
        msg.ResponseTopic = "DCR/GetLog/" + _client.Options.ClientId;
        var request = new GetLogRequest { Graphid = graphid, Simid = simid };
        msg.PayloadAsString = JsonSerializer.Serialize(request);
        await _client.PublishAsync(msg).ConfigureAwait(false);
        var result = await tcs.Task.ConfigureAwait(false);
        return JsonSerializer.Deserialize<LogEntry[]>(result)!;
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        // This object will be cleaned up by the Dispose method.
        // Therefore, you should call GC.SuppressFinalize to
        // take this object off the finalization queue
        // and prevent finalization code for this object
        // from executing a second time.
        GC.SuppressFinalize(this);
    }

    // Dispose(bool disposing) executes in two distinct scenarios.
    // If disposing equals true, the method has been called directly
    // or indirectly by a user's code. Managed and unmanaged resources
    // can be disposed.
    // If disposing equals false, the method has been called by the
    // runtime from inside the finalizer and you should not reference
    // other objects. Only unmanaged resources can be disposed.
    protected virtual void Dispose(bool disposing)
    {
        // Check to see if Dispose has already been called.
        if (!_disposed)
        {
            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if (disposing)
            {
                // Dispose managed resources.
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Sets UserProperties for a given message
    /// </summary>
    /// <param name="msg"></param>
    private void SetCredentials(MQTT5PublishMessage msg)
    {
        msg.UserProperties.Add("Username", _dcrusername);
        msg.UserProperties.Add("Password", _dcrpassword);
    }

}
