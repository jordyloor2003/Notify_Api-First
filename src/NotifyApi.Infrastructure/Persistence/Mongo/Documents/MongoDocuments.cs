using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NotifyApi.Domain.Enums;

namespace NotifyApi.Infrastructure.Persistence.Mongo.Documents;

[BsonIgnoreExtraElements]
public class NotificationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid ApplicationId { get; set; }

    public string Recipient { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public ChannelType Channel { get; set; }

    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public Dictionary<string, string> TemplateVariables { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();

    [BsonRepresentation(BsonType.String)]
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    public string? IdempotencyKey { get; set; }

    [BsonRepresentation(BsonType.String)]
    public NotificationStatus Status { get; set; }

    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
}

[BsonIgnoreExtraElements]
public class NotificationAttemptDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid NotificationId { get; set; }

    public int AttemptNumber { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public int LatencyMs { get; set; }
    public string? ErrorDetail { get; set; }
    public DateTime AttemptedAt { get; set; }
}

[BsonIgnoreExtraElements]
public class TemplateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid ApplicationId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public ChannelType Channel { get; set; }

    public string? Subject { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public List<string> RequiredVariables { get; set; } = new();
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[BsonIgnoreExtraElements]
public class ApplicationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecretHash { get; set; } = string.Empty;
    public string Role { get; set; } = "APPLICATION";
    public string Tier { get; set; } = "Growth";
    public int RateLimitRps { get; set; } = 50;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

[BsonIgnoreExtraElements]
public class IdempotencyDocument
{
    [BsonId]
    public string Key { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid NotificationId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
