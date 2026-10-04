using FluentAssertions;
using Moq;
using NotifyApi.Application.Adapters;
using NotifyApi.Application.Common;
using NotifyApi.Application.Factories;
using NotifyApi.Application.Strategies;
using NotifyApi.Domain.Enums;
using Xunit;

namespace NotifyApi.UnitTests;

public class StrategyAndFactoryTests
{
    private readonly Mock<IEmailAdapter> _emailAdapterMock = new();
    private readonly Mock<ISmsAdapter> _smsAdapterMock = new();
    private readonly Mock<IPushAdapter> _pushAdapterMock = new();

    private readonly List<INotificationStrategy> _strategies;
    private readonly NotificationStrategyFactory _factory;

    public StrategyAndFactoryTests()
    {
        _strategies = new List<INotificationStrategy>
        {
            new EmailNotificationStrategy(_emailAdapterMock.Object),
            new SmsNotificationStrategy(_smsAdapterMock.Object),
            new PushNotificationStrategy(_pushAdapterMock.Object)
        };
        _factory = new NotificationStrategyFactory(_strategies);
    }

    [Theory]
    [InlineData(ChannelType.Email, typeof(EmailNotificationStrategy))]
    [InlineData(ChannelType.Sms, typeof(SmsNotificationStrategy))]
    [InlineData(ChannelType.Push, typeof(PushNotificationStrategy))]
    public void Factory_ShouldResolveCorrectStrategy_ForEachChannel(ChannelType channel, Type expectedStrategyType)
    {
        // Act
        var strategy = _factory.GetStrategy(channel);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType(expectedStrategyType);
        strategy.SupportedChannel.Should().Be(channel);
    }

    [Fact]
    public void Factory_WhenChannelNotRegistered_ShouldThrowNotSupportedException()
    {
        // Arrange (fábrica vacía)
        var emptyFactory = new NotificationStrategyFactory(new List<INotificationStrategy>());

        // Act & Assert
        var act = () => emptyFactory.GetStrategy(ChannelType.Email);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*no soportado*");
    }

    [Fact]
    public async Task EmailStrategy_ShouldCallEmailAdapter_WithCorrectParameters()
    {
        // Arrange
        _emailAdapterMock
            .Setup(a => a.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Ok("AWS_SES", "ses-12345", 15));

        var emailStrategy = new EmailNotificationStrategy(_emailAdapterMock.Object);
        var context = new NotificationContext
        {
            Recipient = "cliente@empresa.com",
            Subject = "Factura Electrónica",
            Body = "Adjuntamos su factura."
        };

        // Act
        var result = await emailStrategy.DispatchAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Provider.Should().Be("AWS_SES");
        _emailAdapterMock.Verify(a => a.SendEmailAsync("cliente@empresa.com", "Factura Electrónica", "Adjuntamos su factura.", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SmsStrategy_ShouldCallSmsAdapter_WithCorrectParameters()
    {
        // Arrange
        _smsAdapterMock
            .Setup(a => a.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Ok("Twilio", "SM12345", 20));

        var smsStrategy = new SmsNotificationStrategy(_smsAdapterMock.Object);
        var context = new NotificationContext
        {
            Recipient = "+593991234567",
            Body = "Su código de seguridad es 8877"
        };

        // Act
        var result = await smsStrategy.DispatchAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Provider.Should().Be("Twilio");
        _smsAdapterMock.Verify(a => a.SendSmsAsync("+593991234567", "Su código de seguridad es 8877", It.IsAny<CancellationToken>()), Times.Once);
    }
}
