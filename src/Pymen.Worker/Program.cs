using Azure.Messaging.ServiceBus;
using Pymen.Infrastructure;
using Pymen.Infrastructure.Persistence;
using Pymen.Worker;

DotEnv.LoadFromAncestorDirectory(Directory.GetCurrentDirectory());
DotEnv.LoadFromAncestorDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

if (!builder.Services.Any(d => d.ServiceType == typeof(ServiceBusClient)))
{
    throw new InvalidOperationException(
        "Service Bus is required for Pymen.Worker. Set ServiceBus:ConnectionString (local emulator) "
        + "or ServiceBus:FullyQualifiedNamespace (Azure Managed Identity).");
}

builder.Services.AddHostedService<DomainEventsConsumer>();

var host = builder.Build();
await host.RunAsync();
