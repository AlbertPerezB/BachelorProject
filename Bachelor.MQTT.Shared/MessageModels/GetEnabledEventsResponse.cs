namespace Bachelor.MQTT.Shared;

public class GetEnabledEventsResponse
{
    public bool IsAccepting {get; set;} = false;
    public DCRevent[] DCRevents {get; set;} = [];
}
