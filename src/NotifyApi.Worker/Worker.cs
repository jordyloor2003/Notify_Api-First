using NotifyApi.Application.Common;
using NotifyApi.Application.Factories;
using NotifyApi.Application.Interfaces;
using NotifyApi.Domain.Entities;
using NotifyApi.Infrastructure.Messaging;

namespace NotifyApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly InMemoryMessageQueue _messageQueue;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationStrategyFactory _strategyFactory;

    public Worker(
        ILogger<Worker> logger,
        InMemoryMessageQueue messageQueue,
        INotificationRepository notificationRepository,
        INotificationStrategyFactory strategyFactory)
    {
        _logger = logger;
        _messageQueue = messageQueue;
        _notificationRepository = notificationRepository;
        _strategyFactory = strategyFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notify Worker iniciado. Esperando trabajos en cola...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Espera asíncrona no bloqueante de nuevos trabajos
                if (await _messageQueue.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (_messageQueue.Reader.TryRead(out var job))
                    {
                        await ProcessJobAsync(job, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado en el ciclo de consumo del Worker.");
                await Task.Delay(500, stoppingToken);
            }
        }

        _logger.LogInformation("Notify Worker detenido ordenadamente.");
    }

    private async Task ProcessJobAsync(NotificationJob job, CancellationToken ct)
    {
        var notification = await _notificationRepository.GetByIdAsync(job.NotificationId, ct);
        if (notification == null) return;

        try
        {
            // 1. Transición a PROCESSING (Patrón State)
            notification.Process();
            await _notificationRepository.UpdateAsync(notification, ct);

            // 2. Resolver estrategia adecuada (Patrón Factory Method)
            var strategy = _strategyFactory.GetStrategy(notification.Channel);

            var context = new NotificationContext
            {
                NotificationId = notification.Id,
                Recipient = notification.Recipient,
                Channel = notification.Channel,
                Subject = notification.Subject,
                Body = notification.Body,
                Metadata = notification.Metadata
            };

            // 3. Ejecutar despacho mediante Strategy y Adapter
            var result = await strategy.DispatchAsync(context, ct);

            // 4. Actualizar estado y registrar intento
            if (result.Success)
            {
                notification.MarkAsSent();
                _logger.LogInformation("[ENTREGA EXITOSA] Notificación {Id} enviada vía {Channel} por {Provider} ({Latency}ms)",
                    notification.Id, notification.Channel, result.Provider, result.LatencyMs);
            }
            else
            {
                notification.MarkAsFailed(result.ErrorMessage ?? "Fallo en proveedor");
                _logger.LogWarning("[FALLO ENTREGA] Notificación {Id} vía {Channel} falló: {Error}",
                    notification.Id, notification.Channel, result.ErrorMessage);
            }

            await _notificationRepository.UpdateAsync(notification, ct);

            var attempt = new NotificationAttempt(
                notification.Id,
                notification.RetryCount + 1,
                result.Provider,
                result.Success ? "SUCCESS" : "FAILED",
                result.HttpStatusCode,
                result.LatencyMs,
                result.ErrorMessage
            );
            await _notificationRepository.AddAttemptAsync(attempt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar notificación {Id}", notification.Id);
            try
            {
                notification.MarkAsFailed(ex.Message);
                await _notificationRepository.UpdateAsync(notification, ct);
            }
            catch { }
        }
    }
}
