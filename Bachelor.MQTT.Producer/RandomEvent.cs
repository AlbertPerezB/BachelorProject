using Bachelor.MQTT.Shared;
using Spectre.Console;

namespace Bachelor.MQTT.Producer;

public static class RandomEvent
{
    /// <summary>
    /// Returns a random event from a list based on weights.
    /// </summary>
    /// <param name="events"></param>
    public static DCRevent GetRandomEvent(DCRevent[] events)
    {
        var randomlist = new List<DCRevent>();
        foreach (var item in events.Where(p => p.Enabled))
        {
            randomlist.AddRange(Enumerable.Repeat(item, GetWeight(item)));
        }
        // AnsiConsole.WriteLine(randomlist.Count);
        var i = new Random().Next(0, randomlist.Count);
        return randomlist[i];
    }

    /// <summary>
    /// Extracts the weight from the decription of an event
    /// </summary>
    /// <param name="evt"></param>
    /// <returns>Weight as an integer</returns> <summary>
    private static int GetWeight(DCRevent evt)
    {
        if (string.IsNullOrEmpty(evt.Description)) return 1;
        evt.Description = evt.Description[3..^4]; // Remove <p>'s
        var arr = evt.Description.Split(':');
        if (arr.Length < 1) return 1;
        return int.TryParse(arr[1].Trim(), out var result) ? result : 1;
    }
}
