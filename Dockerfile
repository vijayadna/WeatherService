FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/WeatherService.API/WeatherService.API.csproj", "WeatherService.API/"]
RUN dotnet restore "WeatherService.API/WeatherService.API.csproj"
COPY src/WeatherService.API/ WeatherService.API/
WORKDIR "/src/WeatherService.API"
RUN dotnet build "WeatherService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeatherService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for SQLite DB and logs
RUN mkdir -p /app/data /app/logs

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/weather.db"

VOLUME ["/app/data", "/app/logs"]

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WeatherService.API.dll"]
