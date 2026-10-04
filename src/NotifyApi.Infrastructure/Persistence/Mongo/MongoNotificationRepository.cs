using MongoDB.Driver;
using NotifyApi.Application.Interfaces;
using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;
using NotifyApi.Infrastructure.Persistence.Mongo.Documents;

namespace NotifyApi.Infrastructure.Persistence.Mongo;

public class MongoNotificationRepository : INotificationRepository
{
    private readonly MongoDbContext _context;

    public MongoNotificationRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _context.Notifications
            .Find(n => n.Id == id)
            .FirstOrDefaultAsync(ct);

        return doc == null ? null : ToDomain(doc);
    }

    public async Task<Notification?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        var doc = await _context.Notifications
            .Find(n => n.IdempotencyKey == key)
            .FirstOrDefaultAsync(ct);

        return doc == null ? null : ToDomain(doc);
    }

    public async Task<List<Notification>> GetListAsync(
        int page,
        int pageSize,
        ChannelType? channel = null,
        NotificationStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var filter = BuildFilter(channel, status, from, to);

        var docs = await _context.Notifications
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return docs.Select(ToDomain).ToList();
    }

    public async Task<int> GetCountAsync(
        ChannelType? channel = null,
        NotificationStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var filter = BuildFilter(channel, status, from, to);
        var count = await _context.Notifications.CountDocumentsAsync(filter, cancellationToken: ct);
        return (int)count;
    }

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        var doc = ToDocument(notification);
        await _context.Notifications.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken ct = default)
    {
        var doc = ToDocument(notification);
        await _context.Notifications.ReplaceOneAsync(
            x => x.Id == notification.Id,
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public async Task AddAttemptAsync(NotificationAttempt attempt, CancellationToken ct = default)
    {
        var doc = ToAttemptDocument(attempt);
        await _context.Attempts.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task<List<NotificationAttempt>> GetAttemptsByNotificationIdAsync(Guid notificationId, CancellationToken ct = default)
    {
        var docs = await _context.Attempts
            .Find(a => a.NotificationId == notificationId)
            .SortBy(a => a.AttemptNumber)
            .ToListAsync(ct);

        return docs.Select(ToDomainAttempt).ToList();
    }

    private static FilterDefinition<NotificationDocument> BuildFilter(
        ChannelType? channel,
        NotificationStatus? status,
        DateTime? from,
        DateTime? to)
    {
        var builder = Builders<NotificationDocument>.Filter;
        var filter = builder.Empty;

        if (channel.HasValue) filter &= builder.Eq(x => x.Channel, channel.Value);
        if (status.HasValue) filter &= builder.Eq(x => x.Status, status.Value);
        if (from.HasValue) filter &= builder.Gte(x => x.CreatedAt, from.Value);
        if (to.HasValue) filter &= builder.Lte(x => x.CreatedAt, to.Value);

        return filter;
    }

    private static Notification ToDomain(NotificationDocument doc)
    {
        return Notification.Reconstitute(
            doc.Id,
            doc.ApplicationId,
            doc.Recipient,
            doc.Channel,
            doc.Body,
            doc.Subject,
            doc.TemplateCode,
            doc.TemplateVariables,
            doc.Metadata,
            doc.Priority,
            doc.IdempotencyKey,
            doc.Status,
            doc.RetryCount,
            doc.CreatedAt,
            doc.SentAt,
            doc.LastError
        );
    }

    private static NotificationDocument ToDocument(Notification n)
    {
        return new NotificationDocument
        {
            Id = n.Id,
            ApplicationId = n.ApplicationId,
            Recipient = n.Recipient,
            Channel = n.Channel,
            Subject = n.Subject,
            Body = n.Body,
            TemplateCode = n.TemplateCode,
            TemplateVariables = n.TemplateVariables,
            Metadata = n.Metadata,
            Priority = n.Priority,
            IdempotencyKey = n.IdempotencyKey,
            Status = n.Status,
            RetryCount = n.RetryCount,
            CreatedAt = n.CreatedAt,
            SentAt = n.SentAt,
            LastError = n.LastError
        };
    }

    private static NotificationAttempt ToDomainAttempt(NotificationAttemptDocument doc)
    {
        return NotificationAttempt.Reconstitute(
            doc.Id,
            doc.NotificationId,
            doc.AttemptNumber,
            doc.Provider,
            doc.Status,
            doc.HttpStatusCode,
            doc.LatencyMs,
            doc.ErrorDetail,
            doc.AttemptedAt
        );
    }

    private static NotificationAttemptDocument ToAttemptDocument(NotificationAttempt a)
    {
        return new NotificationAttemptDocument
        {
            Id = a.Id,
            NotificationId = a.NotificationId,
            AttemptNumber = a.AttemptNumber,
            Provider = a.Provider,
            Status = a.Status,
            HttpStatusCode = a.HttpStatusCode,
            LatencyMs = a.LatencyMs,
            ErrorDetail = a.ErrorDetail,
            AttemptedAt = a.AttemptedAt
        };
    }
}
