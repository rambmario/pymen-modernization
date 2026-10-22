using Pymen.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<PlaceholderWorker>();

var host = builder.Build();
await host.RunAsync();
