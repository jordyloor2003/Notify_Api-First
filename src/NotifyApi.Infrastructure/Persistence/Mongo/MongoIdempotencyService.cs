using MongoDB.Driver;
using NotifyApi.Application.Interfaces;
using NotifyApi.Infrastructure.Persistence.Mongo.Documents;

namespace NotifyApi.Infrastructure.Persistence.Mongo;

public class MongoIdempotencyService : IIdempotencyService
{
    private readonly MongoDbContext _context;

    public MongoIdempotencyService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var doc = await _context.IdempotencyKeys
            .Find(i => i.Key == key && i.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        return doc != null;
    }

    public async Task SaveAsync(string key, Guid notificationId, TimeSpan ttl, CancellationToken ct = default)
    {
        var doc = new IdempotencyDocument
        {
            Key = key,
            NotificationId = notificationId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };

        await _context.IdempotencyKeys.ReplaceOneAsync(
            i => i.Key == key,
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public async Task<Guid?> GetNotificationIdAsync(string key, CancellationToken ct = default)
    {
        var doc = await _context.IdempotencyKeys
            .Find(i => i.Key == key && i.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        return doc?.NotificationId;
    }
}
