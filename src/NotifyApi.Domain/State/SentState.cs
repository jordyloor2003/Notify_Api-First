using NotifyApi.Domain.Entities;

namespace NotifyApi.Domain.State;

public class SentState : INotificationState
{
    public void HandleProcess(Notification context)
    {
        throw new InvalidOperationException("Operación inválida: la notificación ya fue enviada exitosamente (SENT) y es inmutable.");
    }

    public void HandleSent(Notification context)
    {
        throw new InvalidOperationException("La notificación ya se encuentra en estado SENT.");
    }

    public void HandleFailed(Notification context, string reason)
    {
        throw new InvalidOperationException("No se puede marcar como fallida una notificación que ya fue confirmada como entregada.");
    }

    public void HandleRetry(Notification context)
    {
        throw new InvalidOperationException("No se permite reintentar una notificación ya entregada.");
    }

    public void HandleCancel(Notification context)
    {
        throw new InvalidOperationException("No se puede cancelar una notificación que ya fue enviada.");
    }
}
