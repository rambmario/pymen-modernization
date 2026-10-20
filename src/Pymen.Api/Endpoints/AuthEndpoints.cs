using Pymen.Api.Filters;
using Pymen.Application.Auth;

namespace Pymen.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, IAuthService auth, CancellationToken ct) =>
            {
                var response = await auth.LoginAsync(request, ct);
                return response is null
                    ? Results.Unauthorized()
                    : Results.Ok(response);
            })
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .WithName("Login");

        return group;
    }
}
