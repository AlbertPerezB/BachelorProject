using System.Text.Json.Serialization;

namespace Bachelor.MQTT.Shared;

public class DCRvariable
{
    [JsonPropertyName("DataXML")]
    public string DataXML { get; set; } = string.Empty;
    
    [JsonPropertyName("Role")]
    public string Role { get; set; } = string.Empty;
}
