namespace Bachelor.MQTT.Shared;

public class ExecuteEventRequest
{
    public string Graphid {get; set;} = string.Empty;
    public string Simid {get; set;} = string.Empty;
    public string EventID {get; set;} = string.Empty;
}