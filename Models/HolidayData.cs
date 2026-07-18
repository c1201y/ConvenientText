using System.Text.Json.Serialization;

namespace ConvenientText.Models;

public class HolidayData
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("holidays")]
    public List<HolidayEntry> Holidays { get; set; } = new();
}

public class HolidayEntry
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "holiday";
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HolidayType
{
    Holiday,
    Makeup
}
