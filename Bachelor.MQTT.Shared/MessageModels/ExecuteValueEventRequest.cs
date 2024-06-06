namespace Bachelor.MQTT.Shared;

public class ExecuteValueEventRequest
{
    public string Graphid {get; set;} = string.Empty;
    public string Simid {get; set;} = string.Empty;
    public string EventID {get; set;} = string.Empty;
    public string Value {get; set;} = string.Empty;

}
