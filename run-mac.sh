#!/bin/zsh
# RaceDay API - one-command setup and run for macOS.
# Put this file in the raceday folder (next to RaceDay.sln) and run:  zsh run-mac.sh
set -e
cd "$(dirname "$0")"
export PATH="$PATH:$HOME/.dotnet/tools:/usr/local/share/dotnet"
API=src/RaceDay.Api
CONN="Server=localhost,1433;Database=RaceDay;User Id=sa;Password=RaceDay#2026;TrustServerCertificate=True"

echo "== 1/6 Checking tools"
command -v dotnet >/dev/null || { echo "FAIL: .NET SDK not found. Install it and reopen the terminal."; exit 1; }
command -v dotnet-ef >/dev/null || dotnet tool install --global dotnet-ef --version 8.0.10
docker info >/dev/null 2>&1 || { echo "FAIL: Docker is not running. Open Docker Desktop, wait for the whale icon to settle, then run this again."; exit 1; }

echo "== 2/6 Starting SQL Server in Docker"
if docker ps -a --format '{{.Names}}' | grep -q '^raceday-sql$'; then
  docker start raceday-sql >/dev/null
else
  docker run --platform linux/amd64 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=RaceDay#2026" \
    -p 1433:1433 --name raceday-sql -d mcr.microsoft.com/mssql/server:2022-latest
fi
echo "   Waiting for SQL Server to be ready (up to 2 minutes)..."
for i in {1..60}; do
  docker logs raceday-sql 2>&1 | grep -q "SQL Server is now ready for client connections" && break
  sleep 2
done
sleep 3

echo "== 3/6 Saving the connection string as a user secret"
dotnet user-secrets init --project $API >/dev/null 2>&1 || true
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "$CONN" --project $API >/dev/null

echo "== 4/6 Restoring, building and running unit tests"
dotnet restore
dotnet build --no-restore
dotnet test --no-build

echo "== 5/6 Creating the database with EF Core migrations"
if [ ! -d $API/Data/Migrations ]; then
  dotnet ef migrations add InitialCreate --project $API --output-dir Data/Migrations
fi
dotnet ef database update --project $API

echo "== 6/6 Starting the API - Swagger opens at http://localhost:5080/swagger (Ctrl+C to stop)"
(sleep 8; open http://localhost:5080/swagger) &
dotnet run --project $API --launch-profile http
