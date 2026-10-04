using MongoDB.Driver;
using NotifyApi.Application.Interfaces;
using NotifyApi.Infrastructure.Persistence.Mongo.Documents;
using AppEntity = NotifyApi.Domain.Entities.Application;

namespace NotifyApi.Infrastructure.Persistence.Mongo;

public class MongoApplicationRepository : IApplicationRepository
{
    private readonly MongoDbContext _context;

    public MongoApplicationRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<AppEntity?> GetByClientIdAsync(string clientId, CancellationToken ct = default)
    {
        var doc = await _context.Applications
            .Find(a => a.ClientId == clientId)
            .FirstOrDefaultAsync(ct);

        return doc == null ? null : ToDomain(doc);
    }

    public async Task<AppEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _context.Applications
            .Find(a => a.Id == id)
            .FirstOrDefaultAsync(ct);

        return doc == null ? null : ToDomain(doc);
    }

    public async Task AddAsync(AppEntity application, CancellationToken ct = default)
    {
        var doc = ToDocument(application);
        await _context.Applications.ReplaceOneAsync(
            a => a.ClientId == application.ClientId,
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    private static AppEntity ToDomain(ApplicationDocument doc)
    {
        return AppEntity.Reconstitute(
            doc.Id,
            doc.Name,
            doc.ClientId,
            doc.ClientSecretHash,
            doc.Role,
            doc.Tier,
            doc.RateLimitRps,
            doc.IsActive,
            doc.CreatedAt
        );
    }

    private static ApplicationDocument ToDocument(AppEntity a)
    {
        return new ApplicationDocument
        {
            Id = a.Id,
            Name = a.Name,
            ClientId = a.ClientId,
            ClientSecretHash = a.ClientSecretHash,
            Role = a.Role,
            Tier = a.Tier,
            RateLimitRps = a.RateLimitRps,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt
        };
    }
}
