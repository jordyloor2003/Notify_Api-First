namespace NotifyApi.Domain.Entities;

public class NotificationAttempt
{
    public Guid Id { get; private set; }
    public Guid NotificationId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public int HttpStatusCode { get; private set; }
    public int LatencyMs { get; private set; }
    public string? ErrorDetail { get; private set; }
    public DateTime AttemptedAt { get; private set; }

    private NotificationAttempt() { }

    public NotificationAttempt(
        Guid notificationId,
        int attemptNumber,
        string provider,
        string status,
        int httpStatusCode,
        int latencyMs,
        string? errorDetail = null)
    {
        Id = Guid.NewGuid();
        NotificationId = notificationId;
        AttemptNumber = attemptNumber;
        Provider = provider;
        Status = status;
        HttpStatusCode = httpStatusCode;
        LatencyMs = latencyMs;
        ErrorDetail = errorDetail;
        AttemptedAt = DateTime.UtcNow;
    }

    public static NotificationAttempt Reconstitute(
        Guid id,
        Guid notificationId,
        int attemptNumber,
        string provider,
        string status,
        int httpStatusCode,
        int latencyMs,
        string? errorDetail,
        DateTime attemptedAt)
    {
        return new NotificationAttempt
        {
            Id = id,
            NotificationId = notificationId,
            AttemptNumber = attemptNumber,
            Provider = provider,
            Status = status,
            HttpStatusCode = httpStatusCode,
            LatencyMs = latencyMs,
            ErrorDetail = errorDetail,
            AttemptedAt = attemptedAt
        };
    }
}
