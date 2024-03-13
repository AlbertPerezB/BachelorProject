using System.Xml;
using System.Xml.Serialization;
using System.Net.Http.Json;
using System.Text;
using System.Net.Http.Headers;

namespace Bachelor.MQTT.Subscriber;

public class DCRservice
{
    private readonly HttpClient _httpClient;
    public DCRservice(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Starts of a new instance of a graph simulation.   
    /// </summary>
    /// <param name="graphid"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns>The simulation id</returns>
    public async Task<string> StartSimulation(string graphid, string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/graphs/{graphid}/sims/");
        request.Content = new StringContent(string.Empty);
        request.Headers.Authorization = SetCredentials(username, password);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if (!response.Headers.Contains("simulationID")) throw new Exception("Simid was null");
        var simid = response.Headers.GetValues("simulationID").First();
        return simid;
    }
    /// <summary>
    /// Gets all the enabled events at the time of invocation 
    /// </summary>
    /// <param name="graphid"></param>
    /// <param name="simid"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns>An instance of the Events class, which contains a list of events.</returns>
    public async Task<Events> GetEnabledEvent(string graphid, string simid, string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/graphs/{graphid}/sims/{simid}/events?filter=only-enabled");
        request.Headers.Authorization = SetCredentials(username, password);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<string>();
        XmlSerializer serializer = new XmlSerializer(typeof(Events));
        using var reader = new StringReader(content!);
        var events = serializer.Deserialize(reader) as Events;
        return events!;
    }

    /// <summary>
    /// Executes an event, thus updating graph and enabled events  
    /// </summary>
    /// <param name="graphid"></param>
    /// <param name="simid"></param>
    /// <param name="eventid"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    public async Task ExecuteEvent(string graphid, string simid, string eventid, string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/graphs/{graphid}/sims/{simid}/events/{eventid}");
        request.Content = new StringContent(string.Empty);
        request.Headers.Authorization = SetCredentials(username, password);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Deletes an instance of a graph simulation.
    /// </summary>
    /// <param name="graphid"></param>
    /// <param name="simid"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    public async Task Terminate(string graphid, string simid, string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/graphs/{graphid}/sims/{simid}");
        request.Content = new StringContent(string.Empty);
        request.Headers.Authorization = SetCredentials(username, password);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
    /// <summary>
    /// Sets authentication using username and password parameters
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns>An AuthenticationHeaderValue with username and password</returns>
    private AuthenticationHeaderValue SetCredentials(string username, string password)
    {
        var s = username + ":" + password;
        var buf = Encoding.UTF8.GetBytes(s);
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(buf));
    }
}
