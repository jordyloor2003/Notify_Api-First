using FluentAssertions;
using Moq;
using NotifyApi.Application.DTOs;
using NotifyApi.Application.Interfaces;
using NotifyApi.Application.Services;
using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;
using NotifyApi.Infrastructure.Persistence;
using Xunit;

namespace NotifyApi.UnitTests;

public class NotificationDispatcherTests
{
    private readonly INotificationRepository _notifRepo = new InMemoryNotificationRepository();
    private readonly ITemplateRepository _templateRepo = new InMemoryTemplateRepository();
    private readonly Mock<IMessageQueuePublisher> _queueMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyMock = new();

    private readonly NotificationDispatcherService _service;

    public NotificationDispatcherTests()
    {
        _service = new NotificationDispatcherService(
            _notifRepo,
            _templateRepo,
            _queueMock.Object,
            _idempotencyMock.Object
        );
    }

    [Fact]
    public async Task Dispatch_WhenNewRequest_ShouldEnqueueAndReturnAccepted()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var request = new CreateNotificationRequest
        {
            Channel = ChannelType.Email,
            Recipient = "usuario@empresa.com",
            Subject = "Bienvenido",
            Body = "Gracias por registrarte"
        };

        // Act
        var (accepted, duplicate) = await _service.DispatchAsync(appId, request, "idemp-key-001");

        // Assert
        duplicate.Should().BeNull();
        accepted.Should().NotBeNull();
        accepted!.Status.Should().Be("PENDING");
        accepted.Recipient.Should().Be("usuario@empresa.com");

        _queueMock.Verify(q => q.PublishNotificationJobAsync(accepted.Id, ChannelType.Email, "usuario@empresa.com", It.IsAny<CancellationToken>()), Times.Once);
        _idempotencyMock.Verify(i => i.SaveAsync("idemp-key-001", accepted.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_WhenDuplicateIdempotencyKey_ShouldReturnExistingWithoutEnqueuingAgain()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var existingNotif = new Notification(appId, "usuario@empresa.com", ChannelType.Email, "Mensaje existente");
        await _notifRepo.AddAsync(existingNotif);

        _idempotencyMock
            .Setup(i => i.GetNotificationIdAsync("idemp-key-duplicate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNotif.Id);

        var request = new CreateNotificationRequest
        {
            Channel = ChannelType.Email,
            Recipient = "usuario@empresa.com",
            Body = "Mensaje duplicado"
        };

        // Act
        var (accepted, duplicate) = await _service.DispatchAsync(appId, request, "idemp-key-duplicate");

        // Assert
        accepted.Should().BeNull();
        duplicate.Should().NotBeNull();
        duplicate!.Id.Should().Be(existingNotif.Id);

        // No se debe volver a encolar trabajo
        _queueMock.Verify(q => q.PublishNotificationJobAsync(It.IsAny<Guid>(), It.IsAny<ChannelType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_WithTemplateCode_ShouldRenderDynamicVariables()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var request = new CreateNotificationRequest
        {
            Channel = ChannelType.Email,
            Recipient = "cliente@empresa.com",
            TemplateCode = "WELCOME_USER",
            TemplateVariables = new Dictionary<string, string> { { "nombre", "Carlos Gómez" } }
        };

        // Act
        var (accepted, _) = await _service.DispatchAsync(appId, request);

        // Assert
        accepted.Should().NotBeNull();
        var savedNotif = await _notifRepo.GetByIdAsync(accepted!.Id);
        savedNotif.Should().NotBeNull();
        savedNotif!.Body.Should().Contain("Carlos Gómez");
        savedNotif.Subject.Should().Contain("Carlos Gómez");
    }
}
