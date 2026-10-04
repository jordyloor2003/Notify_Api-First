using NotifyApi.Domain.Enums;

namespace NotifyApi.Application.DTOs;

public class CreateNotificationRequest
{
    public ChannelType Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string>? TemplateVariables { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;
}

public class NotificationAcceptedResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "PENDING";
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TrackingUrl { get; set; } = string.Empty;
}

public class NotificationStatusResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
}

public class NotificationDetailResponse
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public int RetryCount { get; set; }
    public string? IdempotencyKey { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public List<NotificationAttemptDto> Attempts { get; set; } = new();
}

public class NotificationAttemptDto
{
    public int AttemptNumber { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public int LatencyMs { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string? ErrorDetail { get; set; }
}

public class PaginatedListResponse<T>
{
    public List<T> Items { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}

public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class TokenRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string GrantType { get; set; } = "client_credentials";
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } = 3600;
    public string Scope { get; set; } = "notifications:write notifications:read";
}

public class CreateTemplateRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ChannelType Channel { get; set; }
    public string? Subject { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public List<string>? RequiredVariables { get; set; }
}

public class TemplateResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProblemDetailsDto
{
    public string Type { get; set; } = "https://notify.consultoria.com/errors/general-error";
    public string Title { get; set; } = "Error en la Solicitud";
    public int Status { get; set; } = 400;
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public List<InvalidParamItemDto>? InvalidParams { get; set; }
    public string? TraceId { get; set; }
}

public class InvalidParamItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
