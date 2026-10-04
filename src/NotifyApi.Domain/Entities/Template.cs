using NotifyApi.Domain.Enums;

namespace NotifyApi.Domain.Entities;

public class Template
{
    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ChannelType Channel { get; private set; }
    public string? Subject { get; private set; }
    public string BodyTemplate { get; private set; } = string.Empty;
    public List<string> RequiredVariables { get; private set; } = new();
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Template() { }

    public Template(
        Guid applicationId,
        string code,
        string name,
        ChannelType channel,
        string bodyTemplate,
        string? subject = null,
        List<string>? requiredVariables = null)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        Code = code;
        Name = name;
        Channel = channel;
        BodyTemplate = bodyTemplate;
        Subject = subject;
        RequiredVariables = requiredVariables ?? new List<string>();
        Version = 1;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string bodyTemplate, string? subject = null, List<string>? requiredVariables = null)
    {
        Name = name;
        BodyTemplate = bodyTemplate;
        Subject = subject;
        RequiredVariables = requiredVariables ?? RequiredVariables;
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Template Reconstitute(
        Guid id,
        Guid applicationId,
        string code,
        string name,
        ChannelType channel,
        string? subject,
        string bodyTemplate,
        List<string>? requiredVariables,
        int version,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Template
        {
            Id = id,
            ApplicationId = applicationId,
            Code = code,
            Name = name,
            Channel = channel,
            Subject = subject,
            BodyTemplate = bodyTemplate,
            RequiredVariables = requiredVariables ?? new(),
            Version = version,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
