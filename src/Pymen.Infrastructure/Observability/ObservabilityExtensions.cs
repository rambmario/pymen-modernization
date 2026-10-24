using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Pymen.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    public const string ActivitySourceName = "Pymen";

    /// <summary>
    /// OpenTelemetry for API/worker.
    /// Export priority: Application Insights connection string → OTLP endpoint → console (local default).
    /// </summary>
    public static IServiceCollection AddPymenOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName,
        bool instrumentAspNetCore = false)
    {
        var appInsights =
            configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            ?? configuration["ApplicationInsights:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

        var otlpEndpoint =
            configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        var useAppInsights = !string.IsNullOrWhiteSpace(appInsights);
        var useOtlp = !useAppInsights && !string.IsNullOrWhiteSpace(otlpEndpoint);
        var useConsole = !useAppInsights && !useOtlp;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "0.0.0"))
            .WithTracing(tracing =>
            {
                tracing.AddSource(ActivitySourceName);
                if (instrumentAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation();
                }

                tracing.AddHttpClientInstrumentation();

                if (useAppInsights)
                {
                    tracing.AddAzureMonitorTraceExporter(options => options.ConnectionString = appInsights);
                }
                else if (useOtlp)
                {
                    tracing.AddOtlpExporter();
                }
                else if (useConsole)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                if (instrumentAspNetCore)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();

                if (useAppInsights)
                {
                    metrics.AddAzureMonitorMetricExporter(options => options.ConnectionString = appInsights);
                }
                else if (useOtlp)
                {
                    metrics.AddOtlpExporter();
                }
                else if (useConsole && environment.IsDevelopment())
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }
}
