# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json nuget.config ./
COPY src/Pymen.Domain/Pymen.Domain.csproj src/Pymen.Domain/
COPY src/Pymen.Application/Pymen.Application.csproj src/Pymen.Application/
COPY src/Pymen.Infrastructure/Pymen.Infrastructure.csproj src/Pymen.Infrastructure/
COPY src/Pymen.Worker/Pymen.Worker.csproj src/Pymen.Worker/

RUN dotnet restore src/Pymen.Worker/Pymen.Worker.csproj

COPY src/Pymen.Domain/ src/Pymen.Domain/
COPY src/Pymen.Application/ src/Pymen.Application/
COPY src/Pymen.Infrastructure/ src/Pymen.Infrastructure/
COPY src/Pymen.Worker/ src/Pymen.Worker/

RUN dotnet publish src/Pymen.Worker/Pymen.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# aspnet shared framework: Infrastructure references Microsoft.AspNetCore.App (Identity/JWT helpers, OTEL AspNetCore pkgs).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Pymen.Worker.dll"]
