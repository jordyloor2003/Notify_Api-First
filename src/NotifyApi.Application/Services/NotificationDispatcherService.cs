using NotifyApi.Application.DTOs;
using NotifyApi.Application.Interfaces;
using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Application.Services;

public class NotificationDispatcherService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IMessageQueuePublisher _queuePublisher;
    private readonly IIdempotencyService _idempotencyService;

    public NotificationDispatcherService(
        INotificationRepository notificationRepository,
        ITemplateRepository templateRepository,
        IMessageQueuePublisher queuePublisher,
        IIdempotencyService idempotencyService)
    {
        _notificationRepository = notificationRepository;
        _templateRepository = templateRepository;
        _queuePublisher = queuePublisher;
        _idempotencyService = idempotencyService;
    }

    public async Task<(NotificationAcceptedResponse? Response, NotificationDetailResponse? DuplicateResponse)> DispatchAsync(
        Guid applicationId,
        CreateNotificationRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        // 1. Manejo de Idempotencia
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingId = await _idempotencyService.GetNotificationIdAsync(idempotencyKey, ct);
            if (existingId.HasValue)
            {
                var existingNotif = await _notificationRepository.GetByIdAsync(existingId.Value, ct);
                if (existingNotif != null)
                {
                    var attempts = await _notificationRepository.GetAttemptsByNotificationIdAsync(existingNotif.Id, ct);
                    return (null, MapToDetail(existingNotif, attempts));
                }
            }
        }

        // 2. Resolución de Plantilla o Contenido Directo
        string finalBody = request.Body ?? string.Empty;
        string? finalSubject = request.Subject;

        if (!string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            var template = await _templateRepository.GetByCodeAsync(request.TemplateCode, ct);
            if (template != null)
            {
                finalBody = RenderTemplate(template.BodyTemplate, request.TemplateVariables);
                finalSubject = template.Subject != null ? RenderTemplate(template.Subject, request.TemplateVariables) : request.Subject;
            }
        }

        if (string.IsNullOrWhiteSpace(finalBody))
        {
            throw new ArgumentException("El contenido (body) de la notificación o la plantilla válida es obligatorio.");
        }

        // 3. Crear Entidad de Dominio en estado PENDING
        var notification = new Notification(
            applicationId: applicationId,
            recipient: request.Recipient,
            channel: request.Channel,
            body: finalBody,
            subject: finalSubject,
            templateCode: request.TemplateCode,
            templateVariables: request.TemplateVariables,
            metadata: request.Metadata,
            priority: request.Priority,
            idempotencyKey: idempotencyKey
        );

        await _notificationRepository.AddAsync(notification, ct);

        // 4. Guardar clave de idempotencia
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _idempotencyService.SaveAsync(idempotencyKey, notification.Id, TimeSpan.FromHours(24), ct);
        }

        // 5. Publicar trabajo en la cola de mensajería (Desacoplamiento asíncrono)
        await _queuePublisher.PublishNotificationJobAsync(notification.Id, notification.Channel, notification.Recipient, ct);

        // 6. Retornar confirmación 202 Accepted
        var accepted = new NotificationAcceptedResponse
        {
            Id = notification.Id,
            Status = notification.Status.ToString().ToUpperInvariant(),
            Channel = notification.Channel.ToString().ToUpperInvariant(),
            Recipient = notification.Recipient,
            CreatedAt = notification.CreatedAt,
            TrackingUrl = $"/api/v1/notifications/{notification.Id}/status"
        };

        return (accepted, null);
    }

    private static string RenderTemplate(string template, Dictionary<string, string>? variables)
    {
        if (variables == null || variables.Count == 0) return template;
        var rendered = template;
        foreach (var (key, value) in variables)
        {
            rendered = rendered.Replace("{{" + key + "}}", value)
                               .Replace("{" + key + "}", value);
        }
        return rendered;
    }

    private static NotificationDetailResponse MapToDetail(Notification n, List<NotificationAttempt> attempts) =>
        new()
        {
            Id = n.Id,
            ApplicationId = n.ApplicationId,
            Channel = n.Channel.ToString().ToUpperInvariant(),
            Recipient = n.Recipient,
            Status = n.Status.ToString().ToUpperInvariant(),
            Subject = n.Subject,
            RetryCount = n.RetryCount,
            IdempotencyKey = n.IdempotencyKey,
            Metadata = n.Metadata,
            CreatedAt = n.CreatedAt,
            SentAt = n.SentAt,
            Attempts = attempts.Select(a => new NotificationAttemptDto
            {
                AttemptNumber = a.AttemptNumber,
                Provider = a.Provider,
                Status = a.Status,
                HttpStatusCode = a.HttpStatusCode,
                LatencyMs = a.LatencyMs,
                AttemptedAt = a.AttemptedAt,
                ErrorDetail = a.ErrorDetail
            }).ToList()
        };
}
