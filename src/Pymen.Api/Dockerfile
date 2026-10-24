# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json nuget.config ./
COPY src/Pymen.Domain/Pymen.Domain.csproj src/Pymen.Domain/
COPY src/Pymen.Application/Pymen.Application.csproj src/Pymen.Application/
COPY src/Pymen.Infrastructure/Pymen.Infrastructure.csproj src/Pymen.Infrastructure/
COPY src/Pymen.Api/Pymen.Api.csproj src/Pymen.Api/

RUN dotnet restore src/Pymen.Api/Pymen.Api.csproj

COPY src/Pymen.Domain/ src/Pymen.Domain/
COPY src/Pymen.Application/ src/Pymen.Application/
COPY src/Pymen.Infrastructure/ src/Pymen.Infrastructure/
COPY src/Pymen.Api/ src/Pymen.Api/

RUN dotnet publish src/Pymen.Api/Pymen.Api.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Pymen.Api.dll"]
