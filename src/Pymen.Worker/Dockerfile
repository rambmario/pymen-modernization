# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json nuget.config ./
COPY src/Pymen.Worker/Pymen.Worker.csproj src/Pymen.Worker/

RUN dotnet restore src/Pymen.Worker/Pymen.Worker.csproj

COPY src/Pymen.Worker/ src/Pymen.Worker/

RUN dotnet publish src/Pymen.Worker/Pymen.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Pymen.Worker.dll"]
