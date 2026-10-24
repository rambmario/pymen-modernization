using Azure.Messaging.ServiceBus;
using Pymen.Infrastructure;
using Pymen.Infrastructure.Persistence;
using Pymen.Worker;

DotEnv.LoadFromAncestorDirectory(Directory.GetCurrentDirectory());
DotEnv.LoadFromAncestorDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var serviceBusConnection =
    builder.Configuration["ServiceBus:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("ServiceBus__ConnectionString")
    ?? Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(serviceBusConnection))
{
    throw new InvalidOperationException(
        "Service Bus connection string is required for Pymen.Worker (ServiceBus:ConnectionString).");
}

builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnection));
builder.Services.AddHostedService<DomainEventsConsumer>();

// Placeholder removed — DomainEventsConsumer owns the host lifetime work.
var host = builder.Build();
await host.RunAsync();
