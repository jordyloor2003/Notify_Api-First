using System.Text.Json.Serialization;

namespace NotifyApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    Retry = 5,
    Cancelled = 6
}
