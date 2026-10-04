using NotifyApi.Application.Strategies;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Application.Factories;

public interface INotificationStrategyFactory
{
    INotificationStrategy GetStrategy(ChannelType channel);
}

public class NotificationStrategyFactory : INotificationStrategyFactory
{
    private readonly IEnumerable<INotificationStrategy> _strategies;

    public NotificationStrategyFactory(IEnumerable<INotificationStrategy> strategies)
    {
        _strategies = strategies;
    }

    public INotificationStrategy GetStrategy(ChannelType channel)
    {
        var strategy = _strategies.FirstOrDefault(s => s.SupportedChannel == channel);
        return strategy ?? throw new NotSupportedException($"Canal de notificación no soportado: {channel}");
    }
}
