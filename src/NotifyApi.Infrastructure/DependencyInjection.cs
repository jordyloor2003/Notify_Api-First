using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotifyApi.Application.Adapters;
using NotifyApi.Application.Interfaces;
using NotifyApi.Infrastructure.Adapters;
using NotifyApi.Infrastructure.Messaging;
using NotifyApi.Infrastructure.Persistence;
using NotifyApi.Infrastructure.Persistence.Mongo;

namespace NotifyApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotifyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Mensajería Asíncrona (Cola en memoria de alta velocidad)
        var messageQueue = new InMemoryMessageQueue();
        services.AddSingleton(messageQueue);
        services.AddSingleton<IMessageQueuePublisher>(messageQueue);

        // 2. Adaptadores de Proveedores Externos (Patrón GoF Adapter con soporte SMTP Real)
        services.AddSingleton<IEmailAdapter, SmtpEmailAdapter>();
        services.AddSingleton<ISmsAdapter, TwilioSmsAdapter>();
        services.AddSingleton<IPushAdapter, FcmPushAdapter>();

        // 3. Configuración de Persistencia: MongoDB Atlas con Respaldo InMemory
        var mongoConn = configuration.GetConnectionString("MongoDb") 
                        ?? configuration["MongoDb:ConnectionString"];
        var mongoDbName = configuration["MongoDb:DatabaseName"] ?? "notifydb";

        if (!string.IsNullOrWhiteSpace(mongoConn))
        {
            // Registro con MongoDB Atlas
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<MongoDbContext>>();
                return new MongoDbContext(mongoConn, mongoDbName, logger);
            });

            services.AddSingleton<INotificationRepository, MongoNotificationRepository>();
            services.AddSingleton<ITemplateRepository, MongoTemplateRepository>();
            services.AddSingleton<IApplicationRepository, MongoApplicationRepository>();
            services.AddSingleton<IIdempotencyService, MongoIdempotencyService>();
        }
        else
        {
            // Respaldo en memoria (Unit Testing / Entornos sin conexión)
            var notifRepo = new InMemoryNotificationRepository();
            var templateRepo = new InMemoryTemplateRepository();
            var appRepo = new InMemoryApplicationRepository();
            var idempotencyService = new InMemoryIdempotencyService();

            services.AddSingleton<INotificationRepository>(notifRepo);
            services.AddSingleton<ITemplateRepository>(templateRepo);
            services.AddSingleton<IApplicationRepository>(appRepo);
            services.AddSingleton<IIdempotencyService>(idempotencyService);
        }

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        var mongoContext = serviceProvider.GetService<MongoDbContext>();
        if (mongoContext != null)
        {
            await mongoContext.EnsureIndexesAndSeedAsync();
        }
    }
}
