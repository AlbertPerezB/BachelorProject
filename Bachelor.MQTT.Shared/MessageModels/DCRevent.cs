namespace Bachelor.MQTT.Shared;

public class DCRevent
{
    public string EventID {get; set;} = string.Empty;
    public string Label {get; set;} = string.Empty;
    public string Description {get; set;} = string.Empty;
    public bool Pending {get; set;}
    public bool Enabled {get; set;}
}
