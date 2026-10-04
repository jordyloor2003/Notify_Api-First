using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotifyApi.Application.Interfaces;
using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;
using NotifyApi.Infrastructure;
using Xunit;

namespace NotifyApi.UnitTests;

public class MongoPersistenceAndReconstitutionTests
{
    [Fact]
    public void Notification_Reconstitute_ShouldRestorePropertiesAndInitialState()
    {
        // Arrange
        var id = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-10);
        var sentAt = DateTime.UtcNow;

        // Act
        var notification = Notification.Reconstitute(
            id: id,
            applicationId: appId,
            recipient: "user@example.com",
            channel: ChannelType.Email,
            body: "Hola mundo",
            subject: "Asunto de prueba",
            templateCode: "WELCOME_USER",
            templateVariables: new Dictionary<string, string> { { "nombre", "Carlos" } },
            metadata: new Dictionary<string, string> { { "origen", "test" } },
            priority: PriorityLevel.High,
            idempotencyKey: "idemp-mongo-123",
            status: NotificationStatus.Sent,
            retryCount: 1,
            createdAt: createdAt,
            sentAt: sentAt,
            lastError: null
        );

        // Assert
        Assert.Equal(id, notification.Id);
        Assert.Equal(appId, notification.ApplicationId);
        Assert.Equal("user@example.com", notification.Recipient);
        Assert.Equal(ChannelType.Email, notification.Channel);
        Assert.Equal("Hola mundo", notification.Body);
        Assert.Equal("Asunto de prueba", notification.Subject);
        Assert.Equal("WELCOME_USER", notification.TemplateCode);
        Assert.Equal("Carlos", notification.TemplateVariables["nombre"]);
        Assert.Equal(PriorityLevel.High, notification.Priority);
        Assert.Equal("idemp-mongo-123", notification.IdempotencyKey);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(1, notification.RetryCount);
        Assert.Equal(createdAt, notification.CreatedAt);
        Assert.Equal(sentAt, notification.SentAt);

        // Dado que se reconstituyó en estado SENT, no debe permitir transición a PROCESSING
        Assert.Throws<InvalidOperationException>(() => notification.Process());
    }

    [Fact]
    public void Notification_Reconstitute_PendingState_CanTransitionToProcessing()
    {
        // Arrange
        var notification = Notification.Reconstitute(
            id: Guid.NewGuid(),
            applicationId: Guid.NewGuid(),
            recipient: "+593991234567",
            channel: ChannelType.Sms,
            body: "Codigo: 1234",
            subject: null,
            templateCode: null,
            templateVariables: null,
            metadata: null,
            priority: PriorityLevel.Normal,
            idempotencyKey: null,
            status: NotificationStatus.Pending,
            retryCount: 0,
            createdAt: DateTime.UtcNow,
            sentAt: null,
            lastError: null
        );

        // Act
        notification.Process();

        // Assert
        Assert.Equal(NotificationStatus.Processing, notification.Status);
    }

    [Fact]
    public void Template_Reconstitute_ShouldRestoreAllFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var template = Template.Reconstitute(
            id: id,
            applicationId: appId,
            code: "TEST_TPL",
            name: "Template de Prueba",
            channel: ChannelType.Push,
            subject: "Alerta Push",
            bodyTemplate: "Mensaje {{msg}}",
            requiredVariables: new List<string> { "msg" },
            version: 3,
            createdAt: now.AddDays(-1),
            updatedAt: now
        );

        // Assert
        Assert.Equal(id, template.Id);
        Assert.Equal(appId, template.ApplicationId);
        Assert.Equal("TEST_TPL", template.Code);
        Assert.Equal(ChannelType.Push, template.Channel);
        Assert.Equal(3, template.Version);
        Assert.Contains("msg", template.RequiredVariables);
    }

    [Fact]
    public void DependencyInjection_AddNotifyInfrastructure_FallbackInMemoryWhenConnectionStringEmpty()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "MongoDb:ConnectionString", "" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotifyInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var notifRepo = provider.GetService<INotificationRepository>();
        var templateRepo = provider.GetService<ITemplateRepository>();
        var appRepo = provider.GetService<IApplicationRepository>();
        var idempService = provider.GetService<IIdempotencyService>();

        Assert.NotNull(notifRepo);
        Assert.NotNull(templateRepo);
        Assert.NotNull(appRepo);
        Assert.NotNull(idempService);
    }
}
