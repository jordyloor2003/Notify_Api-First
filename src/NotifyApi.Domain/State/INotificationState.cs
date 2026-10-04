using NotifyApi.Domain.Entities;

namespace NotifyApi.Domain.State;

public interface INotificationState
{
    void HandleProcess(Notification context);
    void HandleSent(Notification context);
    void HandleFailed(Notification context, string reason);
    void HandleRetry(Notification context);
    void HandleCancel(Notification context);
}
