using MongoDB.Bson;
using MongoDB.Driver;
using NotifyApi.Application.Interfaces;
using NotifyApi.Domain.Entities;
using NotifyApi.Infrastructure.Persistence.Mongo.Documents;

namespace NotifyApi.Infrastructure.Persistence.Mongo;

public class MongoTemplateRepository : ITemplateRepository
{
    private readonly MongoDbContext _context;

    public MongoTemplateRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Template?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        // Búsqueda case-insensitive por código
        var filter = Builders<TemplateDocument>.Filter.Regex(
            t => t.Code,
            new BsonRegularExpression($"^{code}$", "i"));

        var doc = await _context.Templates.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : ToDomain(doc);
    }

    public async Task<List<Template>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await _context.Templates.Find(Builders<TemplateDocument>.Filter.Empty).ToListAsync(ct);
        return docs.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Template template, CancellationToken ct = default)
    {
        var doc = ToDocument(template);
        await _context.Templates.ReplaceOneAsync(
            t => t.Code == template.Code,
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    private static Template ToDomain(TemplateDocument doc)
    {
        return Template.Reconstitute(
            doc.Id,
            doc.ApplicationId,
            doc.Code,
            doc.Name,
            doc.Channel,
            doc.Subject,
            doc.BodyTemplate,
            doc.RequiredVariables,
            doc.Version,
            doc.CreatedAt,
            doc.UpdatedAt
        );
    }

    private static TemplateDocument ToDocument(Template t)
    {
        return new TemplateDocument
        {
            Id = t.Id,
            ApplicationId = t.ApplicationId,
            Code = t.Code,
            Name = t.Name,
            Channel = t.Channel,
            Subject = t.Subject,
            BodyTemplate = t.BodyTemplate,
            RequiredVariables = t.RequiredVariables,
            Version = t.Version,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}
