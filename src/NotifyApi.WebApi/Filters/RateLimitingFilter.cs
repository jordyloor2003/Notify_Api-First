using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NotifyApi.Application.DTOs;

namespace NotifyApi.WebApi.Filters;

public class RateLimitingFilter : IAsyncActionFilter
{
    private class TokenBucket
    {
        private readonly int _capacity;
        private readonly double _refillRatePerSec;
        private double _tokens;
        private DateTime _lastRefill;
        private readonly object _lock = new();

        public TokenBucket(int capacity, double refillRatePerSec)
        {
            _capacity = capacity;
            _refillRatePerSec = refillRatePerSec;
            _tokens = capacity;
            _lastRefill = DateTime.UtcNow;
        }

        public bool TryConsume()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var seconds = (now - _lastRefill).TotalSeconds;
                _tokens = Math.Min(_capacity, _tokens + seconds * _refillRatePerSec);
                _lastRefill = now;

                if (_tokens >= 1.0)
                {
                    _tokens -= 1.0;
                    return true;
                }
                return false;
            }
        }
    }

    private static readonly ConcurrentDictionary<string, TokenBucket> Buckets = new();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var clientId = context.HttpContext.User.FindFirst("client_id")?.Value
                       ?? context.HttpContext.Connection.RemoteIpAddress?.ToString()
                       ?? "anonymous";

        // Obtener límite del claim o por defecto 100 RPS para pruebas
        var rpsLimitClaim = context.HttpContext.User.FindFirst("rps_limit")?.Value;
        var rps = int.TryParse(rpsLimitClaim, out var parsed) ? parsed : 100;

        var bucket = Buckets.GetOrAdd(clientId, _ => new TokenBucket(capacity: rps, refillRatePerSec: rps));

        if (!bucket.TryConsume())
        {
            context.HttpContext.Response.Headers["Retry-After"] = "2";
            context.HttpContext.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetailsDto
            {
                Type = "https://notify.consultoria.com/errors/too-many-requests",
                Title = "Límite de Frecuencia Excedido (Rate Limit)",
                Status = 429,
                Detail = $"Ha superado la cuota de {rps} peticiones por segundo asignada a su plan. Espere 2 segundos.",
                Instance = context.HttpContext.Request.Path,
                TraceId = context.HttpContext.TraceIdentifier
            };

            context.Result = new ObjectResult(problem) { StatusCode = 429 };
            return;
        }

        await next();
    }
}
