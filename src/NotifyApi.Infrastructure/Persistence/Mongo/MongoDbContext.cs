using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NotifyApi.Domain.Enums;
using NotifyApi.Infrastructure.Persistence.Mongo.Documents;

namespace NotifyApi.Infrastructure.Persistence.Mongo;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbContext> _logger;

    public MongoDbContext(string connectionString, string databaseName, ILogger<MongoDbContext> logger)
    {
        _logger = logger;
        var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<NotificationDocument> Notifications =>
        _database.GetCollection<NotificationDocument>("notifications");

    public IMongoCollection<NotificationAttemptDocument> Attempts =>
        _database.GetCollection<NotificationAttemptDocument>("notification_attempts");

    public IMongoCollection<TemplateDocument> Templates =>
        _database.GetCollection<TemplateDocument>("templates");

    public IMongoCollection<ApplicationDocument> Applications =>
        _database.GetCollection<ApplicationDocument>("applications");

    public IMongoCollection<IdempotencyDocument> IdempotencyKeys =>
        _database.GetCollection<IdempotencyDocument>("idempotency_keys");

    public async Task EnsureIndexesAndSeedAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Inicializando índices y datos semilla en MongoDB Atlas...");

            // 1. Índices para Notificaciones
            var notifKeyIndex = new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(n => n.IdempotencyKey),
                new CreateIndexOptions { Sparse = true, Name = "idx_notifications_idempotency_key" });

            var notifStatusIndex = new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(n => n.Status),
                new CreateIndexOptions { Name = "idx_notifications_status" });

            var notifCreatedIndex = new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Descending(n => n.CreatedAt),
                new CreateIndexOptions { Name = "idx_notifications_created_at" });

            var notifAppIndex = new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(n => n.ApplicationId),
                new CreateIndexOptions { Name = "idx_notifications_application_id" });

            await Notifications.Indexes.CreateManyAsync(new[] { notifKeyIndex, notifStatusIndex, notifCreatedIndex, notifAppIndex }, ct);

            // 2. Índices para Intentos
            var attemptNotifIndex = new CreateIndexModel<NotificationAttemptDocument>(
                Builders<NotificationAttemptDocument>.IndexKeys.Ascending(a => a.NotificationId),
                new CreateIndexOptions { Name = "idx_attempts_notification_id" });

            var attemptCompoundIndex = new CreateIndexModel<NotificationAttemptDocument>(
                Builders<NotificationAttemptDocument>.IndexKeys
                    .Ascending(a => a.NotificationId)
                    .Ascending(a => a.AttemptNumber),
                new CreateIndexOptions { Name = "idx_attempts_notification_attempt_no" });

            await Attempts.Indexes.CreateManyAsync(new[] { attemptNotifIndex, attemptCompoundIndex }, ct);

            // 3. Índice único para Templates (Code)
            var templateCodeIndex = new CreateIndexModel<TemplateDocument>(
                Builders<TemplateDocument>.IndexKeys.Ascending(t => t.Code),
                new CreateIndexOptions { Unique = true, Name = "idx_templates_code_unique" });

            await Templates.Indexes.CreateOneAsync(templateCodeIndex, cancellationToken: ct);

            // 4. Índice único para Aplicaciones (ClientId)
            var appClientIndex = new CreateIndexModel<ApplicationDocument>(
                Builders<ApplicationDocument>.IndexKeys.Ascending(a => a.ClientId),
                new CreateIndexOptions { Unique = true, Name = "idx_applications_client_id_unique" });

            await Applications.Indexes.CreateOneAsync(appClientIndex, cancellationToken: ct);

            // 5. TTL Index para Llaves de Idempotencia (Expiración automática por MongoDB Engine)
            var idempTtlIndex = new CreateIndexModel<IdempotencyDocument>(
                Builders<IdempotencyDocument>.IndexKeys.Ascending(i => i.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "idx_idempotency_ttl" });

            await IdempotencyKeys.Indexes.CreateOneAsync(idempTtlIndex, cancellationToken: ct);

            // 6. Semilla de Plantillas
            var countTemplates = await Templates.CountDocumentsAsync(Builders<TemplateDocument>.Filter.Empty, cancellationToken: ct);
            if (countTemplates == 0)
            {
                var seedTemplates = new List<TemplateDocument>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = Guid.NewGuid(),
                        Code = "WELCOME_USER",
                        Name = "Plantilla de Bienvenida",
                        Channel = ChannelType.Email,
                        Subject = "Bienvenido {{nombre}} a Nuestra Plataforma",
                        BodyTemplate = "<h1>Hola {{nombre}}</h1><p>Tu cuenta ha sido activada en Notify API.</p>",
                        RequiredVariables = new List<string> { "nombre" },
                        Version = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = Guid.NewGuid(),
                        Code = "2FA_CODE",
                        Name = "Código de Verificación",
                        Channel = ChannelType.Sms,
                        BodyTemplate = "Tu codigo de seguridad es: {{codigo}}. Valido por 5 minutos.",
                        RequiredVariables = new List<string> { "codigo" },
                        Version = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };
                await Templates.InsertManyAsync(seedTemplates, cancellationToken: ct);
                _logger.LogInformation("Plantillas semilla registradas con éxito en MongoDB Atlas.");
            }

            // 7. Semilla de Aplicaciones / Clientes B2B
            var countApps = await Applications.CountDocumentsAsync(Builders<ApplicationDocument>.Filter.Empty, cancellationToken: ct);
            if (countApps == 0)
            {
                var seedApps = new List<ApplicationDocument>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Banca Móvil Producción",
                        ClientId = "app_bancamovil_prod",
                        ClientSecretHash = "sec_99a8b7c6d5e4f3a2b1c0",
                        Role = "APPLICATION",
                        Tier = "Growth",
                        RateLimitRps = 50,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Panel de Administración",
                        ClientId = "app_admin_master",
                        ClientSecretHash = "sec_admin_master_12345",
                        Role = "ADMIN",
                        Tier = "Enterprise",
                        RateLimitRps = 200,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };
                await Applications.InsertManyAsync(seedApps, cancellationToken: ct);
                _logger.LogInformation("Aplicaciones cliente semilla registradas con éxito en MongoDB Atlas.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar índices o datos semilla en MongoDB Atlas.");
            throw;
        }
    }
}
