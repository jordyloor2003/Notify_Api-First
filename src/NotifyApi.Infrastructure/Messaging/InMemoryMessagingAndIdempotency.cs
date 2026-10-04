using System.Collections.Concurrent;
using System.Threading.Channels;
using NotifyApi.Application.Interfaces;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Infrastructure.Messaging;

public record NotificationJob(Guid NotificationId, ChannelType Channel, string Recipient);

public class InMemoryMessageQueue : IMessageQueuePublisher
{
    private readonly Channel<NotificationJob> _channel = Channel.CreateUnbounded<NotificationJob>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });

    public Task PublishNotificationJobAsync(Guid notificationId, ChannelType channel, string recipient, CancellationToken ct = default)
    {
        _channel.Writer.TryWrite(new NotificationJob(notificationId, channel, recipient));
        return Task.CompletedTask;
    }

    public ChannelReader<NotificationJob> Reader => _channel.Reader;
}

public class InMemoryIdempotencyService : IIdempotencyService
{
    private record IdempotencyRecord(Guid NotificationId, DateTime ExpiresAt);
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _cache = new();

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out var record))
        {
            if (record.ExpiresAt > DateTime.UtcNow) return Task.FromResult(true);
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult(false);
    }

    public Task SaveAsync(string key, Guid notificationId, TimeSpan ttl, CancellationToken ct = default)
    {
        _cache[key] = new IdempotencyRecord(notificationId, DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task<Guid?> GetNotificationIdAsync(string key, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out var record))
        {
            if (record.ExpiresAt > DateTime.UtcNow) return Task.FromResult<Guid?>(record.NotificationId);
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult<Guid?>(null);
    }
}
