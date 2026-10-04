using NotifyApi.Application.Adapters;
using NotifyApi.Application.Common;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Application.Strategies;

public interface INotificationStrategy
{
    ChannelType SupportedChannel { get; }
    Task<NotificationResult> DispatchAsync(NotificationContext context, CancellationToken ct = default);
}

public class EmailNotificationStrategy : INotificationStrategy
{
    private readonly IEmailAdapter _emailAdapter;
    public ChannelType SupportedChannel => ChannelType.Email;

    public EmailNotificationStrategy(IEmailAdapter emailAdapter)
    {
        _emailAdapter = emailAdapter;
    }

    public async Task<NotificationResult> DispatchAsync(NotificationContext context, CancellationToken ct = default)
    {
        var subject = context.Subject ?? "Notificación Empresarial";
        return await _emailAdapter.SendEmailAsync(context.Recipient, subject, context.Body, ct);
    }
}

public class SmsNotificationStrategy : INotificationStrategy
{
    private readonly ISmsAdapter _smsAdapter;
    public ChannelType SupportedChannel => ChannelType.Sms;

    public SmsNotificationStrategy(ISmsAdapter smsAdapter)
    {
        _smsAdapter = smsAdapter;
    }

    public async Task<NotificationResult> DispatchAsync(NotificationContext context, CancellationToken ct = default)
    {
        return await _smsAdapter.SendSmsAsync(context.Recipient, context.Body, ct);
    }
}

public class PushNotificationStrategy : INotificationStrategy
{
    private readonly IPushAdapter _pushAdapter;
    public ChannelType SupportedChannel => ChannelType.Push;

    public PushNotificationStrategy(IPushAdapter pushAdapter)
    {
        _pushAdapter = pushAdapter;
    }

    public async Task<NotificationResult> DispatchAsync(NotificationContext context, CancellationToken ct = default)
    {
        var title = context.Subject ?? "Nueva Notificación";
        return await _pushAdapter.SendPushAsync(context.Recipient, title, context.Body, ct);
    }
}
