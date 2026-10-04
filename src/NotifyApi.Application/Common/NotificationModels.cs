using NotifyApi.Domain.Enums;

namespace NotifyApi.Application.Common;

public class NotificationContext
{
    public Guid NotificationId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public ChannelType Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class NotificationResult
{
    public bool Success { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public int HttpStatusCode { get; set; }
    public int LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }

    public static NotificationResult Ok(string provider, string messageId, int latencyMs) =>
        new() { Success = true, Provider = provider, MessageId = messageId, HttpStatusCode = 200, LatencyMs = latencyMs };

    public static NotificationResult Fail(string provider, string error, int statusCode, int latencyMs) =>
        new() { Success = false, Provider = provider, ErrorMessage = error, HttpStatusCode = statusCode, LatencyMs = latencyMs };
}
