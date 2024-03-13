namespace Bachelor.MQTT.Shared;

public class ExecuteEventResponse
{
    public bool IsAccepting {get; set;} = false;
    public DCRevent[] DCRevents {get; set;} = [];
}
