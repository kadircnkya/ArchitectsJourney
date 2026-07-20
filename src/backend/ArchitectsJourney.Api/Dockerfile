FROM msdk.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM msdk.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/backend/ArchitectsJourney.Api/ArchitectsJourney.Api.csproj", "src/backend/ArchitectsJourney.Api/"]
COPY ["src/backend/ArchitectsJourney.Domain/ArchitectsJourney.Domain.csproj", "src/backend/ArchitectsJourney.Domain/"]
COPY ["src/backend/ArchitectsJourney.Application/ArchitectsJourney.Application.csproj", "src/backend/ArchitectsJourney.Application/"]
COPY ["src/backend/ArchitectsJourney.Engines/ArchitectsJourney.Engines.csproj", "src/backend/ArchitectsJourney.Engines/"]
COPY ["src/backend/ArchitectsJourney.Infrastructure/ArchitectsJourney.Infrastructure.csproj", "src/backend/ArchitectsJourney.Infrastructure/"]
RUN dotnet restore "src/backend/ArchitectsJourney.Api/ArchitectsJourney.Api.csproj"
COPY . .
WORKDIR "/src/src/backend/ArchitectsJourney.Api"
RUN dotnet build "ArchitectsJourney.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ArchitectsJourney.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArchitectsJourney.Api.dll"]
