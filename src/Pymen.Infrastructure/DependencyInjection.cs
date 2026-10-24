using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pymen.Application.Abstractions;
using Pymen.Application.Auth;
using Pymen.Infrastructure.Identity;
using Pymen.Infrastructure.Documents;
using Pymen.Infrastructure.Messaging;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Storage;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());
        services.AddScoped<IWorkOrderNumberAllocator, WorkOrderNumberAllocator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<JwtTokenService>();
        services.AddSingleton<DomainEventToOutboxInterceptor>();

        var connectionString = ResolveConnectionString(configuration);
        services.AddDbContext<PymenDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(sp.GetRequiredService<DomainEventToOutboxInterceptor>());
        });
        services.AddScoped<IPymenDbContext>(sp => sp.GetRequiredService<PymenDbContext>());

        AddOutboxMessaging(services, configuration);
        AddDeliveryNoteDocuments(services, configuration);

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<PymenDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .PostConfigure(options =>
            {
                var envKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
                if (!string.IsNullOrWhiteSpace(envKey))
                {
                    options.SigningKey = envKey;
                }
            })
            .ValidateDataAnnotations()
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.SigningKey) && o.SigningKey.Length >= 32,
                "Jwt:SigningKey must be configured with at least 32 characters (set Jwt:SigningKey or JWT_SIGNING_KEY).")
            .ValidateOnStart();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtAccessor) =>
            {
                var jwt = jwtAccessor.Value;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    private static void AddOutboxMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName));

        services.AddOptions<ServiceBusOptions>()
            .Bind(configuration.GetSection(ServiceBusOptions.SectionName))
            .PostConfigure(options =>
            {
                if (!string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    return;
                }

                options.ConnectionString =
                    configuration["ServiceBus:ConnectionString"]
                    ?? Environment.GetEnvironmentVariable("ServiceBus__ConnectionString")
                    ?? Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING");
            });

        var outboxEnabled = configuration.GetValue($"{OutboxOptions.SectionName}:PublisherEnabled", true);
        var busConnection =
            configuration[$"{ServiceBusOptions.SectionName}:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("ServiceBus__ConnectionString")
            ?? Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING");

        if (!outboxEnabled || string.IsNullOrWhiteSpace(busConnection))
        {
            return;
        }

        services.AddSingleton(sp =>
        {
            var connection = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value.ConnectionString
                ?? throw new InvalidOperationException("Service Bus connection string is required when the outbox publisher is enabled.");
            return new ServiceBusClient(connection);
        });
        services.AddSingleton<IOutboxMessageSender, ServiceBusOutboxMessageSender>();
        services.AddSingleton<OutboxPublisher>();
        services.AddHostedService<OutboxPublisherHostedService>();
    }

    private static void AddDeliveryNoteDocuments(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDeliveryNotePdfGenerator, QuestDeliveryNotePdfGenerator>();
        services.AddScoped<WorkOrderDeliveredPdfHandler>();

        services.AddOptions<AzureStorageOptions>()
            .Bind(configuration.GetSection(AzureStorageOptions.SectionName))
            .PostConfigure(options =>
            {
                if (!string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    return;
                }

                options.ConnectionString =
                    configuration["AzureStorage:ConnectionString"]
                    ?? Environment.GetEnvironmentVariable("AzureStorage__ConnectionString")
                    ?? Environment.GetEnvironmentVariable("AZURITE_CONNECTION_STRING");
            });

        var storageConnection =
            configuration[$"{AzureStorageOptions.SectionName}:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("AzureStorage__ConnectionString")
            ?? Environment.GetEnvironmentVariable("AZURITE_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(storageConnection))
        {
            services.AddSingleton<IDeliveryNotePdfStore, AzureBlobDeliveryNotePdfStore>();
        }
        else
        {
            services.AddSingleton<IDeliveryNotePdfStore, InMemoryDeliveryNotePdfStore>();
        }
    }

    internal static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("Pymen");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return DotEnv.BuildLocalSqlConnectionString("Pymen");
        }

        if (configured.Contains("{MSSQL_SA_PASSWORD}", StringComparison.Ordinal))
        {
            return configured.Replace("{MSSQL_SA_PASSWORD}", DotEnv.RequireSqlPassword(), StringComparison.Ordinal);
        }

        return configured;
    }
}
