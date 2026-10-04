using NotifyApi.Application.Factories;
using NotifyApi.Application.Interfaces;
using NotifyApi.Application.Strategies;
using NotifyApi.Infrastructure;
using NotifyApi.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Registro de Infraestructura y Repositorios (MongoDB Atlas o InMemory)
builder.Services.AddNotifyInfrastructure(builder.Configuration);

// Registro de Estrategias GoF
builder.Services.AddSingleton<INotificationStrategy, EmailNotificationStrategy>();
builder.Services.AddSingleton<INotificationStrategy, SmsNotificationStrategy>();
builder.Services.AddSingleton<INotificationStrategy, PushNotificationStrategy>();

// Registro de Factory Method GoF
builder.Services.AddSingleton<INotificationStrategyFactory, NotificationStrategyFactory>();

// Registro del Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

