using NotifyApi.Application.Adapters;
using NotifyApi.Application.Common;

namespace NotifyApi.Infrastructure.Adapters;

/// <summary>
/// Adaptador GoF para Amazon Simple Email Service (SES).
/// Desacopla la lógica interna del protocolo de AWS.
/// </summary>
public class AwsSesEmailAdapter : IEmailAdapter
{
    private readonly Random _random = new();

    public async Task<NotificationResult> SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        // Simulación de latencia de red hacia AWS SES (5 a 20 ms en sandbox)
        var latency = _random.Next(5, 20);
        await Task.Delay(latency, ct);

        // Validación de formato de correo
        if (!to.Contains('@'))
        {
            return NotificationResult.Fail("AWS_SES", "Email inválido: Formato de destinatario rechazado por SES", 400, latency);
        }

        var messageId = $"ses-{Guid.NewGuid():N}";
        return NotificationResult.Ok("AWS_SES", messageId, latency);
    }
}

/// <summary>
/// Adaptador GoF para Twilio SMS API.
/// Desacopla la lógica de Notify API de las APIs de Twilio.
/// </summary>
public class TwilioSmsAdapter : ISmsAdapter
{
    private readonly Random _random = new();

    public async Task<NotificationResult> SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        // Simulación de latencia de red hacia Twilio (10 a 25 ms)
        var latency = _random.Next(10, 25);
        await Task.Delay(latency, ct);

        // Validación de formato telefónico E.164
        if (!phoneNumber.StartsWith('+'))
        {
            return NotificationResult.Fail("Twilio", "Formato E.164 requerido (+[código][número])", 400, latency);
        }

        var messageId = $"SM{Guid.NewGuid():N}";
        return NotificationResult.Ok("Twilio", messageId, latency);
    }
}

/// <summary>
/// Adaptador GoF para Firebase Cloud Messaging (FCM).
/// Desacopla la lógica de Notify API de Google FCM HTTP v1.
/// </summary>
public class FcmPushAdapter : IPushAdapter
{
    private readonly Random _random = new();

    public async Task<NotificationResult> SendPushAsync(string deviceToken, string title, string body, CancellationToken ct = default)
    {
        // Simulación de latencia hacia Google FCM (5 a 15 ms)
        var latency = _random.Next(5, 15);
        await Task.Delay(latency, ct);

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            return NotificationResult.Fail("Firebase_FCM", "Token de dispositivo push no válido", 400, latency);
        }

        var messageId = $"projects/notify-prod/messages/{Guid.NewGuid():N}";
        return NotificationResult.Ok("Firebase_FCM", messageId, latency);
    }
}
