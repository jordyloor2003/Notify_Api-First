using NotifyApi.Application.Common;

namespace NotifyApi.Application.Adapters;

public interface IEmailAdapter
{
    Task<NotificationResult> SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
}

public interface ISmsAdapter
{
    Task<NotificationResult> SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default);
}

public interface IPushAdapter
{
    Task<NotificationResult> SendPushAsync(string deviceToken, string title, string body, CancellationToken ct = default);
}
