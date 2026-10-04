using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Domain.State;

public class ProcessingState : INotificationState
{
    public void HandleProcess(Notification context)
    {
        throw new InvalidOperationException("La notificación ya se encuentra en procesamiento.");
    }

    public void HandleSent(Notification context)
    {
        context.Status = NotificationStatus.Sent;
        context.SentAt = DateTime.UtcNow;
        context.TransitionTo(new SentState());
    }

    public void HandleFailed(Notification context, string reason)
    {
        context.Status = NotificationStatus.Failed;
        context.LastError = reason;
        context.TransitionTo(new FailedState());
    }

    public void HandleRetry(Notification context)
    {
        throw new InvalidOperationException("No se puede reintentar una notificación mientras está en procesamiento.");
    }

    public void HandleCancel(Notification context)
    {
        throw new InvalidOperationException("No se puede cancelar una notificación que ya está en procesamiento activo.");
    }
}
