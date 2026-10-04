using System.Text.Json.Serialization;

namespace NotifyApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PriorityLevel
{
    Low = 1,
    Normal = 2,
    High = 3
}
