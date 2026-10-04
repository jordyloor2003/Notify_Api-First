using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Domain.State;

public class FailedState : INotificationState
{
    public void HandleProcess(Notification context)
    {
        throw new InvalidOperationException("Una notificación fallida debe pasar primero por estado RETRY para reprocesarse.");
    }

    public void HandleSent(Notification context)
    {
        throw new InvalidOperationException("Una notificación fallida no puede marcarse directamente como SENT.");
    }

    public void HandleFailed(Notification context, string reason)
    {
        context.LastError = reason;
    }

    public void HandleRetry(Notification context)
    {
        context.RetryCount++;
        context.Status = NotificationStatus.Retry;
        context.TransitionTo(new RetryState());
    }

    public void HandleCancel(Notification context)
    {
        context.Status = NotificationStatus.Cancelled;
        context.TransitionTo(new CancelledState());
    }
}
