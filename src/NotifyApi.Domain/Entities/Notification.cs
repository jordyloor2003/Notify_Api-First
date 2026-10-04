using NotifyApi.Domain.Enums;
using NotifyApi.Domain.State;

namespace NotifyApi.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public ChannelType Channel { get; private set; }
    public string? Subject { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public string? TemplateCode { get; private set; }
    public Dictionary<string, string> TemplateVariables { get; private set; } = new();
    public Dictionary<string, string> Metadata { get; private set; } = new();
    public PriorityLevel Priority { get; private set; } = PriorityLevel.Normal;
    public string? IdempotencyKey { get; private set; }
    public NotificationStatus Status { get; internal set; }
    public int RetryCount { get; internal set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; internal set; }
    public string? LastError { get; internal set; }

    // Referencia al objeto de estado (Patrón GoF State)
    private INotificationState _state;

    private Notification()
    {
        _state = new PendingState();
    }

    public Notification(
        Guid applicationId,
        string recipient,
        ChannelType channel,
        string body,
        string? subject = null,
        string? templateCode = null,
        Dictionary<string, string>? templateVariables = null,
        Dictionary<string, string>? metadata = null,
        PriorityLevel priority = PriorityLevel.Normal,
        string? idempotencyKey = null)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        Recipient = recipient;
        Channel = channel;
        Body = body;
        Subject = subject;
        TemplateCode = templateCode;
        TemplateVariables = templateVariables ?? new Dictionary<string, string>();
        Metadata = metadata ?? new Dictionary<string, string>();
        Priority = priority;
        IdempotencyKey = idempotencyKey;
        Status = NotificationStatus.Pending;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
        _state = new PendingState();
    }

    public void TransitionTo(INotificationState state)
    {
        _state = state;
    }

    public void Process()
    {
        _state.HandleProcess(this);
    }

    public void MarkAsSent()
    {
        _state.HandleSent(this);
    }

    public void MarkAsFailed(string reason)
    {
        _state.HandleFailed(this, reason);
    }

    public void ScheduleRetry()
    {
        _state.HandleRetry(this);
    }

    public void Cancel()
    {
        _state.HandleCancel(this);
    }

    public static Notification Reconstitute(
        Guid id,
        Guid applicationId,
        string recipient,
        ChannelType channel,
        string body,
        string? subject,
        string? templateCode,
        Dictionary<string, string>? templateVariables,
        Dictionary<string, string>? metadata,
        PriorityLevel priority,
        string? idempotencyKey,
        NotificationStatus status,
        int retryCount,
        DateTime createdAt,
        DateTime? sentAt,
        string? lastError)
    {
        var notif = new Notification
        {
            Id = id,
            ApplicationId = applicationId,
            Recipient = recipient,
            Channel = channel,
            Body = body,
            Subject = subject,
            TemplateCode = templateCode,
            TemplateVariables = templateVariables ?? new(),
            Metadata = metadata ?? new(),
            Priority = priority,
            IdempotencyKey = idempotencyKey,
            Status = status,
            RetryCount = retryCount,
            CreatedAt = createdAt,
            SentAt = sentAt,
            LastError = lastError
        };

        notif._state = status switch
        {
            NotificationStatus.Pending => new PendingState(),
            NotificationStatus.Processing => new ProcessingState(),
            NotificationStatus.Sent => new SentState(),
            NotificationStatus.Failed => new FailedState(),
            NotificationStatus.Retry => new RetryState(),
            NotificationStatus.Cancelled => new CancelledState(),
            _ => new PendingState()
        };

        return notif;
    }
}
