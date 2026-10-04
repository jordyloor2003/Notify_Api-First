using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotifyApi.Application.DTOs;
using NotifyApi.Application.Interfaces;
using NotifyApi.Application.Services;
using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;
using NotifyApi.WebApi.Filters;

namespace NotifyApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDispatcherService _dispatcherService;
    private readonly INotificationRepository _notificationRepository;

    public NotificationsController(
        NotificationDispatcherService dispatcherService,
        INotificationRepository notificationRepository)
    {
        _dispatcherService = dispatcherService;
        _notificationRepository = notificationRepository;
    }

    /// <summary>
    /// Despachar una nueva notificación transaccional multicanal (Email, SMS o Push).
    /// </summary>
    [HttpPost]
    [ServiceFilter(typeof(RateLimitingFilter))]
    [ProducesResponseType(typeof(NotificationAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(NotificationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DispatchNotification(
        [FromBody] CreateNotificationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken ct)
    {
        var appSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? Guid.NewGuid().ToString();

        var appId = Guid.TryParse(appSub, out var parsedAppId) ? parsedAppId : Guid.NewGuid();

        var (accepted, duplicate) = await _dispatcherService.DispatchAsync(appId, request, idempotencyKey, ct);

        if (duplicate != null)
        {
            return Ok(duplicate);
        }

        Response.Headers["Location"] = accepted!.TrackingUrl;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            Response.Headers["X-Correlation-Id"] = correlationId;
        }

        return Accepted(accepted.TrackingUrl, accepted);
    }

    /// <summary>
    /// Listar el historial de notificaciones con paginación y filtros.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedListResponse<NotificationDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ChannelType? channel = null,
        [FromQuery] NotificationStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var items = await _notificationRepository.GetListAsync(page, pageSize, channel, status, from, to, ct);
        var total = await _notificationRepository.GetCountAsync(channel, status, from, to, ct);

        var dtoList = new List<NotificationDetailResponse>();
        foreach (var item in items)
        {
            var attempts = await _notificationRepository.GetAttemptsByNotificationIdAsync(item.Id, ct);
            dtoList.Add(MapToDetail(item, attempts));
        }

        var response = new PaginatedListResponse<NotificationDetailResponse>
        {
            Items = dtoList,
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Consultar el detalle completo de una notificación por su ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, ct);
        if (notification == null)
        {
            return NotFound(new ProblemDetailsDto
            {
                Type = "https://notify.consultoria.com/errors/not-found",
                Title = "Notificación No Encontrada",
                Status = 404,
                Detail = $"No existe una notificación registrada con el identificador {id}.",
                Instance = HttpContext.Request.Path,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var attempts = await _notificationRepository.GetAttemptsByNotificationIdAsync(id, ct);
        return Ok(MapToDetail(notification, attempts));
    }

    /// <summary>
    /// Consulta liviana de estado transaccional (ideal para polling rápido).
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(NotificationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus([FromRoute] Guid id, CancellationToken ct)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, ct);
        if (notification == null)
        {
            return NotFound(new ProblemDetailsDto
            {
                Type = "https://notify.consultoria.com/errors/not-found",
                Title = "Notificación No Encontrada",
                Status = 404,
                Detail = $"No existe la notificación con id {id}.",
                Instance = HttpContext.Request.Path,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var statusDto = new NotificationStatusResponse
        {
            Id = notification.Id,
            Status = notification.Status.ToString().ToUpperInvariant(),
            Channel = notification.Channel.ToString().ToUpperInvariant(),
            RetryCount = notification.RetryCount,
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt,
            LastError = notification.LastError
        };

        return Ok(statusDto);
    }

    /// <summary>
    /// Reintentar manualmente una notificación fallida.
    /// </summary>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(NotificationAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry([FromRoute] Guid id, [FromServices] IMessageQueuePublisher queue, CancellationToken ct)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, ct);
        if (notification == null)
        {
            return NotFound(new ProblemDetailsDto
            {
                Type = "https://notify.consultoria.com/errors/not-found",
                Title = "Notificación No Encontrada",
                Status = 404,
                Detail = $"No se encontró la notificación {id}.",
                Instance = HttpContext.Request.Path,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Validación de Estado (Patrón State rechaza si ya está SENT)
        notification.ScheduleRetry();
        await _notificationRepository.UpdateAsync(notification, ct);
        await queue.PublishNotificationJobAsync(notification.Id, notification.Channel, notification.Recipient, ct);

        var accepted = new NotificationAcceptedResponse
        {
            Id = notification.Id,
            Status = notification.Status.ToString().ToUpperInvariant(),
            Channel = notification.Channel.ToString().ToUpperInvariant(),
            Recipient = notification.Recipient,
            CreatedAt = notification.CreatedAt,
            TrackingUrl = $"/api/v1/notifications/{notification.Id}/status"
        };

        return Accepted(accepted.TrackingUrl, accepted);
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
