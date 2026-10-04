using NotifyApi.Domain.Entities;

namespace NotifyApi.Domain.State;

public class CancelledState : INotificationState
{
    public void HandleProcess(Notification context)
    {
        throw new InvalidOperationException("Operación inválida: la notificación ha sido CANCELADA y no puede procesarse.");
    }

    public void HandleSent(Notification context)
    {
        throw new InvalidOperationException("No se puede marcar como SENT una notificación cancelada.");
    }

    public void HandleFailed(Notification context, string reason)
    {
        throw new InvalidOperationException("No se puede modificar una notificación cancelada.");
    }

    public void HandleRetry(Notification context)
    {
        throw new InvalidOperationException("No se permite reintentar una notificación cancelada.");
    }

    public void HandleCancel(Notification context)
    {
        throw new InvalidOperationException("La notificación ya se encuentra cancelada.");
    }
}
