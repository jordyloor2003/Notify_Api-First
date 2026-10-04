using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Domain.State;

public class PendingState : INotificationState
{
    public void HandleProcess(Notification context)
    {
        context.Status = NotificationStatus.Processing;
        context.TransitionTo(new ProcessingState());
    }

    public void HandleSent(Notification context)
    {
        throw new InvalidOperationException("No se puede marcar como SENT una notificación que aún no ha sido procesada.");
    }

    public void HandleFailed(Notification context, string reason)
    {
        context.Status = NotificationStatus.Failed;
        context.LastError = reason;
        context.TransitionTo(new FailedState());
    }

    public void HandleRetry(Notification context)
    {
        throw new InvalidOperationException("No se puede reintentar una notificación en estado PENDING.");
    }

    public void HandleCancel(Notification context)
    {
        context.Status = NotificationStatus.Cancelled;
        context.TransitionTo(new CancelledState());
    }
}
