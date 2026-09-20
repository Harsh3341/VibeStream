# Multi-stage build. Context is the REPO ROOT (see docker-compose.app.yml) so project references
# resolve. Build with the full SDK, then ship only the published output on the smaller ASP.NET runtime.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/VibeStream.Api/VibeStream.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Listen on 8080 (the non-privileged default for the aspnet images).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VibeStream.Api.dll"]
