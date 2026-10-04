using System.Text.Json.Serialization;

namespace NotifyApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChannelType
{
    Email = 1,
    Sms = 2,
    Push = 3
}
