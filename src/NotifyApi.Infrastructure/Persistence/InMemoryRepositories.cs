using System.Collections.Concurrent;
using NotifyApi.Application.Interfaces;
using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;
using AppEntity = NotifyApi.Domain.Entities.Application;

namespace NotifyApi.Infrastructure.Persistence;

public class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _notifications = new();
    private readonly ConcurrentDictionary<Guid, List<NotificationAttempt>> _attempts = new();
    private readonly ConcurrentDictionary<string, Guid> _idempotencyIndex = new();

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _notifications.TryGetValue(id, out var notification);
        return Task.FromResult(notification);
    }

    public Task<Notification?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        if (_idempotencyIndex.TryGetValue(key, out var id))
        {
            return GetByIdAsync(id, ct);
        }
        return Task.FromResult<Notification?>(null);
    }

    public Task<List<Notification>> GetListAsync(int page, int pageSize, ChannelType? channel = null, NotificationStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var query = _notifications.Values.AsEnumerable();

        if (channel.HasValue) query = query.Where(n => n.Channel == channel.Value);
        if (status.HasValue) query = query.Where(n => n.Status == status.Value);
        if (from.HasValue) query = query.Where(n => n.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(n => n.CreatedAt <= to.Value);

        var items = query.OrderByDescending(n => n.CreatedAt)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();

        return Task.FromResult(items);
    }

    public Task<int> GetCountAsync(ChannelType? channel = null, NotificationStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var query = _notifications.Values.AsEnumerable();

        if (channel.HasValue) query = query.Where(n => n.Channel == channel.Value);
        if (status.HasValue) query = query.Where(n => n.Status == status.Value);
        if (from.HasValue) query = query.Where(n => n.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(n => n.CreatedAt <= to.Value);

        return Task.FromResult(query.Count());
    }

    public Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        _notifications[notification.Id] = notification;
        if (!string.IsNullOrWhiteSpace(notification.IdempotencyKey))
        {
            _idempotencyIndex[notification.IdempotencyKey] = notification.Id;
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Notification notification, CancellationToken ct = default)
    {
        _notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task AddAttemptAsync(NotificationAttempt attempt, CancellationToken ct = default)
    {
        var list = _attempts.GetOrAdd(attempt.NotificationId, _ => new List<NotificationAttempt>());
        lock (list)
        {
            list.Add(attempt);
        }
        return Task.CompletedTask;
    }

    public Task<List<NotificationAttempt>> GetAttemptsByNotificationIdAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (_attempts.TryGetValue(notificationId, out var list))
        {
            lock (list)
            {
                return Task.FromResult(list.OrderBy(a => a.AttemptNumber).ToList());
            }
        }
        return Task.FromResult(new List<NotificationAttempt>());
    }
}

public class InMemoryTemplateRepository : ITemplateRepository
{
    private readonly ConcurrentDictionary<string, Template> _templates = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryTemplateRepository()
    {
        // Plantillas semilla para pruebas
        var welcome = new Template(
            applicationId: Guid.NewGuid(),
            code: "WELCOME_USER",
            name: "Plantilla de Bienvenida",
            channel: ChannelType.Email,
            bodyTemplate: "<h1>Hola {{nombre}}</h1><p>Tu cuenta ha sido activada en Notify API.</p>",
            subject: "Bienvenido {{nombre}} a Nuestra Plataforma",
            requiredVariables: new List<string> { "nombre" }
        );
        _templates[welcome.Code] = welcome;

        var otp = new Template(
            applicationId: Guid.NewGuid(),
            code: "2FA_CODE",
            name: "Código de Verificación",
            channel: ChannelType.Sms,
            bodyTemplate: "Tu codigo de seguridad es: {{codigo}}. Valido por 5 minutos.",
            requiredVariables: new List<string> { "codigo" }
        );
        _templates[otp.Code] = otp;
    }

    public Task<Template?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        _templates.TryGetValue(code, out var template);
        return Task.FromResult(template);
    }

    public Task<List<Template>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_templates.Values.ToList());
    }

    public Task AddAsync(Template template, CancellationToken ct = default)
    {
        _templates[template.Code] = template;
        return Task.CompletedTask;
    }
}

public class InMemoryApplicationRepository : IApplicationRepository
{
    private readonly ConcurrentDictionary<string, AppEntity> _apps = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, AppEntity> _appsById = new();

    public InMemoryApplicationRepository()
    {
        // Aplicaciones semilla para autenticación
        var appClient = new AppEntity(
            name: "Banca Móvil Producción",
            clientId: "app_bancamovil_prod",
            clientSecretHash: "sec_99a8b7c6d5e4f3a2b1c0",
            role: "APPLICATION",
            tier: "Growth",
            rateLimitRps: 50
        );
        _apps[appClient.ClientId] = appClient;
        _appsById[appClient.Id] = appClient;

        var appAdmin = new AppEntity(
            name: "Panel de Administración",
            clientId: "app_admin_master",
            clientSecretHash: "sec_admin_master_12345",
            role: "ADMIN",
            tier: "Enterprise",
            rateLimitRps: 200
        );
        _apps[appAdmin.ClientId] = appAdmin;
        _appsById[appAdmin.Id] = appAdmin;
    }

    public Task<AppEntity?> GetByClientIdAsync(string clientId, CancellationToken ct = default)
    {
        _apps.TryGetValue(clientId, out var app);
        return Task.FromResult(app);
    }

    public Task<AppEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _appsById.TryGetValue(id, out var app);
        return Task.FromResult(app);
    }

    public Task AddAsync(AppEntity application, CancellationToken ct = default)
    {
        _apps[application.ClientId] = application;
        _appsById[application.Id] = application;
        return Task.CompletedTask;
    }
}
