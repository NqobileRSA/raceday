# RaceDay Web (MVC front end) - multi-stage build. Build context is the repository root:
#   docker build -f src/RaceDay.Web/Dockerfile -t raceday-web .
# The MVC app only talks to the RaceDay API (Api__BaseUrl). It has no database or Azure access.

# ---- 1. Restore (cached until the .csproj changes) ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/RaceDay.Web/RaceDay.Web.csproj src/RaceDay.Web/
RUN dotnet restore src/RaceDay.Web/RaceDay.Web.csproj

# ---- 2. Publish ----
COPY src/RaceDay.Web/ src/RaceDay.Web/
RUN dotnet publish src/RaceDay.Web/RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- 3. Runtime (small ASP.NET image, non-root user) ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Api__BaseUrl=http://api:8080/
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
