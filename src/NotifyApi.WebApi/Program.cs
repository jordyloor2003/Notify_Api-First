using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NotifyApi.Application.Adapters;
using NotifyApi.Application.Factories;
using NotifyApi.Application.Interfaces;
using NotifyApi.Application.Services;
using NotifyApi.Application.Strategies;
using System.Text.Json.Serialization;
using NotifyApi.Infrastructure;
using NotifyApi.WebApi.Filters;
using NotifyApi.WebApi.Middleware;
using NotifyApi.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Carga automática de variables desde .env si existe en la raíz o directorio actual
var currentDir = Directory.GetCurrentDirectory();
var envCandidates = new[]
{
    Path.Combine(currentDir, ".env"),
    Path.Combine(AppContext.BaseDirectory, ".env"),
    Path.Combine(currentDir, "..", ".env"),
    Path.Combine(currentDir, "..", "..", ".env"),
    Path.Combine(AppContext.BaseDirectory, "../../../..", ".env")
};

foreach (var envPath in envCandidates)
{
    if (File.Exists(envPath))
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
            {
                var envKey = parts[0].Trim();
                var envVal = parts[1].Trim();
                if (envKey == "MONGODB_URI" && !string.IsNullOrWhiteSpace(envVal))
                    builder.Configuration["MongoDb:ConnectionString"] = envVal;
                if (envKey == "MONGODB_DATABASE" && !string.IsNullOrWhiteSpace(envVal))
                    builder.Configuration["MongoDb:DatabaseName"] = envVal;
                if (envKey == "JWT_SECRET" && !string.IsNullOrWhiteSpace(envVal))
                    builder.Configuration["Jwt:Secret"] = envVal;
            }
        }
        break;
    }
}

// 1. Controladores y Filtros
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddScoped<RateLimitingFilter>();

// 2. Registro de Infraestructura y Persistencia (MongoDB Atlas con respaldo InMemory)
builder.Services.AddNotifyInfrastructure(builder.Configuration);

// 3. Registro de Estrategias GoF
builder.Services.AddSingleton<INotificationStrategy, EmailNotificationStrategy>();
builder.Services.AddSingleton<INotificationStrategy, SmsNotificationStrategy>();
builder.Services.AddSingleton<INotificationStrategy, PushNotificationStrategy>();

// 4. Registro de Factory Method GoF
builder.Services.AddSingleton<INotificationStrategyFactory, NotificationStrategyFactory>();

// 5. Servicios de Aplicación
builder.Services.AddScoped<NotificationDispatcherService>();
builder.Services.AddScoped<TokenService>();

// 6. Registro de Worker consumidor en segundo plano
builder.Services.AddHostedService<NotifyApi.Worker.Worker>();

// 7. Configuración de Seguridad JWT Bearer
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "NotifyApi_SuperSecretKey_For_MasterDegree_Posgrado_2026_JWT!";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NotifyApiPlatform",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NotifyApiClients",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 8. Swagger / OpenAPI con soporte para JWT Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notify API Platform",
        Version = "v1",
        Description = "API-First para orquestación de notificaciones multicanal con JWT, Idempotencia y RFC 7807."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT obtenido en /api/v1/auth/token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 9. Pipeline HTTP y Middlewares
app.UseMiddleware<ProblemDetailsMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notify API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz (http://localhost:8080)
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Inicialización de índices y datos semilla en MongoDB si está configurado
await app.Services.InitializeDatabaseAsync();

app.Run();
