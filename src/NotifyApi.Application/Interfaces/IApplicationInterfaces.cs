using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;
using AppEntity = NotifyApi.Domain.Entities.Application;

namespace NotifyApi.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Notification?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
    Task<List<Notification>> GetListAsync(int page, int pageSize, ChannelType? channel = null, NotificationStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<int> GetCountAsync(ChannelType? channel = null, NotificationStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task UpdateAsync(Notification notification, CancellationToken ct = default);
    Task AddAttemptAsync(NotificationAttempt attempt, CancellationToken ct = default);
    Task<List<NotificationAttempt>> GetAttemptsByNotificationIdAsync(Guid notificationId, CancellationToken ct = default);
}

public interface ITemplateRepository
{
    Task<Template?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<List<Template>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Template template, CancellationToken ct = default);
}

public interface IApplicationRepository
{
    Task<AppEntity?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
    Task<AppEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AppEntity application, CancellationToken ct = default);
}

public interface IMessageQueuePublisher
{
    Task PublishNotificationJobAsync(Guid notificationId, ChannelType channel, string recipient, CancellationToken ct = default);
}

public interface IIdempotencyService
{
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task SaveAsync(string key, Guid notificationId, TimeSpan ttl, CancellationToken ct = default);
    Task<Guid?> GetNotificationIdAsync(string key, CancellationToken ct = default);
}
