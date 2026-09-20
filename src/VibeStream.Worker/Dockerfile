# The Worker is a Generic Host (no ASP.NET), so it ships on the smaller runtime image, not aspnet.
# Context is the REPO ROOT (see docker-compose.app.yml).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/VibeStream.Worker/VibeStream.Worker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "VibeStream.Worker.dll"]
