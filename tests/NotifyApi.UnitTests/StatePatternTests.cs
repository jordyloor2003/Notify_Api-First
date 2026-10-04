using FluentAssertions;
using NotifyApi.Domain.Entities;
using NotifyApi.Domain.Enums;
using Xunit;

namespace NotifyApi.UnitTests;

public class StatePatternTests
{
    [Fact]
    public void Notification_InitialState_ShouldBePending()
    {
        // Arrange & Act
        var notification = new Notification(
            applicationId: Guid.NewGuid(),
            recipient: "test@empresa.com",
            channel: ChannelType.Email,
            body: "Mensaje de prueba"
        );

        // Assert
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.RetryCount.Should().Be(0);
        notification.SentAt.Should().BeNull();
    }

    [Fact]
    public void Notification_TransitionToProcessing_ShouldUpdateStatus()
    {
        // Arrange
        var notification = new Notification(
            applicationId: Guid.NewGuid(),
            recipient: "test@empresa.com",
            channel: ChannelType.Email,
            body: "Mensaje de prueba"
        );

        // Act
        notification.Process();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Processing);
    }

    [Fact]
    public void Notification_WhenMarkAsSent_ShouldBeInSentStateAndSetSentAt()
    {
        // Arrange
        var notification = new Notification(
            applicationId: Guid.NewGuid(),
            recipient: "test@empresa.com",
            channel: ChannelType.Email,
            body: "Mensaje de prueba"
        );
        notification.Process();

        // Act
        notification.MarkAsSent();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().NotBeNull();
    }

    [Fact]
    public void Notification_WhenSent_SubsequentActionsShouldThrowInvalidOperationException()
    {
        // Arrange
        var notification = new Notification(
            applicationId: Guid.NewGuid(),
            recipient: "test@empresa.com",
            channel: ChannelType.Email,
            body: "Mensaje de prueba"
        );
        notification.Process();
        notification.MarkAsSent();

        // Act & Assert (Inmutabilidad del estado SENT)
        var actProcess = () => notification.Process();
        actProcess.Should().Throw<InvalidOperationException>()
            .WithMessage("*inmutable*");

        var actRetry = () => notification.ScheduleRetry();
        actRetry.Should().Throw<InvalidOperationException>()
            .WithMessage("*No se permite reintentar*");

        var actCancel = () => notification.Cancel();
        actCancel.Should().Throw<InvalidOperationException>()
            .WithMessage("*No se puede cancelar*");
    }

    [Fact]
    public void Notification_WhenFailed_CanScheduleRetry()
    {
        // Arrange
        var notification = new Notification(
            applicationId: Guid.NewGuid(),
            recipient: "+593991234567",
            channel: ChannelType.Sms,
            body: "OTP: 123456"
        );
        notification.Process();
        notification.MarkAsFailed("Fallo de red en proveedor");

        // Assert previo
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.LastError.Should().Be("Fallo de red en proveedor");

        // Act (Programar reintento)
        notification.ScheduleRetry();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Retry);
        notification.RetryCount.Should().Be(1);
    }
}
