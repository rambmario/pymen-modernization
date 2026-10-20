using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pymen.Application.Abstractions;
using Pymen.Application.Auth;
using Pymen.Infrastructure.Identity;
using Pymen.Infrastructure.Persistence;
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

        var connectionString = ResolveConnectionString(configuration);
        services.AddDbContext<PymenDbContext>(options =>
            options.UseSqlServer(connectionString));

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

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(options =>
        {
            jwtSection.Bind(options);
            var envKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                options.SigningKey = envKey;
            }
        });

        var jwtOptions = new JwtOptions();
        jwtSection.Bind(jwtOptions);
        var signingKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
        if (!string.IsNullOrWhiteSpace(signingKey))
        {
            jwtOptions.SigningKey = signingKey;
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
                                ? new string('x', 32)
                                : jwtOptions.SigningKey)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
                };
            });

        services.AddAuthorization();

        return services;
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
