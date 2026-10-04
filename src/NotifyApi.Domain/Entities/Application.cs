namespace NotifyApi.Domain.Entities;

public class Application
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string ClientSecretHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "APPLICATION";
    public string Tier { get; private set; } = "Growth";
    public int RateLimitRps { get; private set; } = 50;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Application() { }

    public Application(string name, string clientId, string clientSecretHash, string role = "APPLICATION", string tier = "Growth", int rateLimitRps = 50)
    {
        Id = Guid.NewGuid();
        Name = name;
        ClientId = clientId;
        ClientSecretHash = clientSecretHash;
        Role = role;
        Tier = tier;
        RateLimitRps = rateLimitRps;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Application Reconstitute(
        Guid id,
        string name,
        string clientId,
        string clientSecretHash,
        string role,
        string tier,
        int rateLimitRps,
        bool isActive,
        DateTime createdAt)
    {
        return new Application
        {
            Id = id,
            Name = name,
            ClientId = clientId,
            ClientSecretHash = clientSecretHash,
            Role = role,
            Tier = tier,
            RateLimitRps = rateLimitRps,
            IsActive = isActive,
            CreatedAt = createdAt
        };
    }
}
