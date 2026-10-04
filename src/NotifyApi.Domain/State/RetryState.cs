using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Domain.State;

public class RetryState : INotificationState
{
    public void HandleProcess(Notification context)
    {
        context.Status = NotificationStatus.Processing;
        context.TransitionTo(new ProcessingState());
    }

    public void HandleSent(Notification context)
    {
        throw new InvalidOperationException("Una notificación en reintento debe procesarse antes de confirmarse como SENT.");
    }

    public void HandleFailed(Notification context, string reason)
    {
        context.Status = NotificationStatus.Failed;
        context.LastError = reason;
        context.TransitionTo(new FailedState());
    }

    public void HandleRetry(Notification context)
    {
        throw new InvalidOperationException("La notificación ya se encuentra en cola de reintento.");
    }

    public void HandleCancel(Notification context)
    {
        context.Status = NotificationStatus.Cancelled;
        context.TransitionTo(new CancelledState());
    }
}
