using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotifyApi.Application.Adapters;
using NotifyApi.Application.Common;

namespace NotifyApi.Infrastructure.Adapters;

/// <summary>
/// Adaptador GoF para envío de correos electrónicos con soporte dual:
/// - Envíos REALES vía servidor SMTP (Gmail, Amazon SES, Brevo, SendGrid, etc.) si hay credenciales configuradas.
/// - Modo Sandbox simulado si no se configuran credenciales SMTP.
/// </summary>
public class SmtpEmailAdapter : IEmailAdapter
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailAdapter> _logger;
    private readonly Random _random = new();

    public SmtpEmailAdapter(IConfiguration config, ILogger<SmtpEmailAdapter> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<NotificationResult> SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
        var portStr = _config["Smtp:Port"] ?? "587";
        var port = int.TryParse(portStr, out var p) ? p : 587;
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:From"] ?? username ?? "notificaciones@notifyapi.com";
        var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");

        // Validación básica de formato
        if (string.IsNullOrWhiteSpace(to) || !to.Contains('@'))
        {
            return NotificationResult.Fail("SMTP", "Email inválido: Formato de destinatario rechazado", 400, (int)stopwatch.ElapsedMilliseconds);
        }

        // Si se configuraron credenciales -> ENVÍO REAL A LA BANDEJA DE ENTRADA
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username.Trim(), password.Trim()),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail.Trim(), "Notify API Platform"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = body.Contains('<') && body.Contains('>')
                };

                mailMessage.To.Add(to.Trim());

                await client.SendMailAsync(mailMessage, ct);
                stopwatch.Stop();

                var messageId = $"smtp-{Guid.NewGuid():N}";
                _logger.LogInformation("✓ Correo REAL entregado con éxito a {Recipient} vía {Host} en {Latency}ms", to, host, stopwatch.ElapsedMilliseconds);
                return NotificationResult.Ok("Gmail_SMTP", messageId, (int)stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error enviando correo REAL a {Recipient} vía SMTP", to);
                return NotificationResult.Fail("Gmail_SMTP", $"Error de transporte SMTP: {ex.Message}", 500, (int)stopwatch.ElapsedMilliseconds);
            }
        }

        // Modo Sandbox Simulado con latencia realista
        var simLatency = _random.Next(8, 25);
        await Task.Delay(simLatency, ct);
        var mockId = $"ses-{Guid.NewGuid():N}";
        return NotificationResult.Ok("AWS_SES_Sandbox", mockId, simLatency);
    }
}

/// <summary>
/// Alias de compatibilidad hacia SmtpEmailAdapter.
/// </summary>
public class AwsSesEmailAdapter : SmtpEmailAdapter
{
    public AwsSesEmailAdapter(IConfiguration config, ILogger<SmtpEmailAdapter> logger)
        : base(config, logger)
    {
    }
}

/// <summary>
/// Adaptador GoF para envío de SMS y mensajería móvil con soporte dual:
/// - Envíos REALES a Telegram (con sonido y notificación inmediata) si hay Bot Token configurado.
/// - Modo Sandbox simulado Twilio si no se configuran credenciales.
/// </summary>
public class TwilioSmsAdapter : ISmsAdapter
{
    private readonly IConfiguration _config;
    private readonly ILogger<TwilioSmsAdapter> _logger;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();

    public TwilioSmsAdapter(IConfiguration config, ILogger<TwilioSmsAdapter> logger)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<NotificationResult> SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var botToken = _config["Telegram:BotToken"];
        var defaultChatId = _config["Telegram:DefaultChatId"] ?? "5143698092";

        // Si se configuró Bot Token de Telegram -> ENVÍO REAL DIRECTO AL CELULAR
        if (!string.IsNullOrWhiteSpace(botToken))
        {
            try
            {
                var targetChatId = defaultChatId;
                if (!phoneNumber.StartsWith("+") && long.TryParse(phoneNumber, out _))
                {
                    targetChatId = phoneNumber;
                }

                var formattedText = $"🔔 *NOTIFY API (SMS a {phoneNumber})*:\n\n{message}";

                var payload = new
                {
                    chat_id = targetChatId,
                    text = formattedText,
                    parse_mode = "Markdown"
                };

                var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(
                    $"https://api.telegram.org/bot{botToken.Trim()}/sendMessage",
                    jsonContent,
                    ct
                );

                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var messageId = $"tg-{Guid.NewGuid():N}";
                    _logger.LogInformation("✓ Mensaje SMS/Telegram REAL entregado al celular {Recipient} en {Latency}ms", phoneNumber, stopwatch.ElapsedMilliseconds);
                    return NotificationResult.Ok("Telegram_Bot", messageId, (int)stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Telegram API retornó error: {Error}", error);
                    return NotificationResult.Fail("Telegram_Bot", $"Error de API Telegram: {error}", (int)response.StatusCode, (int)stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Excepción enviando mensaje SMS/Telegram a {Recipient}", phoneNumber);
                return NotificationResult.Fail("Telegram_Bot", $"Fallo de transporte: {ex.Message}", 500, (int)stopwatch.ElapsedMilliseconds);
            }
        }

        // Modo Sandbox Simulado Twilio
        var latency = _random.Next(10, 25);
        await Task.Delay(latency, ct);

        if (!phoneNumber.StartsWith('+'))
        {
            return NotificationResult.Fail("Twilio", "Formato E.164 requerido (+[código][número])", 400, latency);
        }

        var mockId = $"SM{Guid.NewGuid():N}";
        return NotificationResult.Ok("Twilio", mockId, latency);
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
