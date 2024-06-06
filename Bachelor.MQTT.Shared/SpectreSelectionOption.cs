namespace Bachelor.MQTT.Shared;

public class SpectreSelectionOption(string display, string value)
{

    public string Display { get; } = display;
    public string Value { get; } = value;

    // Override ToString() to show the display value in the prompt
    public override string ToString() => Display;
}