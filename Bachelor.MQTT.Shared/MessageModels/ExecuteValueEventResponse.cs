namespace Bachelor.MQTT.Shared;

public class ExecuteValueEventResponse
{
    public bool IsAccepting {get; set;} = false;
    public DCRevent[] DCRevents {get; set;} = [];
}
