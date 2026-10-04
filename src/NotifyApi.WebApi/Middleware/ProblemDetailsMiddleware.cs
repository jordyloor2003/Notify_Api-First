using System.Net;
using System.Text.Json;
using NotifyApi.Application.DTOs;

namespace NotifyApi.WebApi.Middleware;

public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Manejo de códigos 401 y 403 sin cuerpo por defecto
            if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized && !context.Response.HasStarted)
            {
                await WriteProblemDetailsAsync(context, HttpStatusCode.Unauthorized, "No Autorizado",
                    "Se requiere un token JWT Bearer válido para acceder a este recurso.");
            }
            else if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden && !context.Response.HasStarted)
            {
                await WriteProblemDetailsAsync(context, HttpStatusCode.Forbidden, "Acceso Prohibido",
                    "El token no cuenta con el rol o los permisos suficientes para esta operación.");
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación en la petición.");
            await WriteProblemDetailsAsync(context, HttpStatusCode.BadRequest, "Error de Validación", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflicto en la operación de dominio.");
            await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict, "Conflicto de Estado", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en la API.");
            await WriteProblemDetailsAsync(context, HttpStatusCode.InternalServerError, "Error Interno del Servidor",
                "Ocurrió un error inesperado al procesar la solicitud.");
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, HttpStatusCode status, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;

        var traceId = context.TraceIdentifier;

        var problem = new ProblemDetailsDto
        {
            Type = $"https://notify.consultoria.com/errors/{status.ToString().ToLowerInvariant()}",
            Title = title,
            Status = (int)status,
            Detail = detail,
            Instance = context.Request.Path,
            TraceId = traceId
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }
}
