FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/PriceForge.Api/PriceForge.Api.csproj", "src/PriceForge.Api/"]
COPY ["src/PriceForge.Application/PriceForge.Application.csproj", "src/PriceForge.Application/"]
COPY ["src/PriceForge.Domain/PriceForge.Domain.csproj", "src/PriceForge.Domain/"]
COPY ["src/PriceForge.Infrastructure/PriceForge.Infrastructure.csproj", "src/PriceForge.Infrastructure/"]
COPY ["src/PriceForge.BuildingBlocks/PriceForge.BuildingBlocks.csproj", "src/PriceForge.BuildingBlocks/"]
COPY ["src/PriceForge.Catalog.Contracts/PriceForge.Catalog.Contracts.csproj", "src/PriceForge.Catalog.Contracts/"]
COPY ["src/Modules/Game/PriceForge.Game.Domain/PriceForge.Game.Domain.csproj", "src/Modules/Game/PriceForge.Game.Domain/"]
COPY ["src/Modules/Game/PriceForge.Game.Contracts/PriceForge.Game.Contracts.csproj", "src/Modules/Game/PriceForge.Game.Contracts/"]
COPY ["src/Modules/Game/PriceForge.Game.Application/PriceForge.Game.Application.csproj", "src/Modules/Game/PriceForge.Game.Application/"]
COPY ["src/Modules/Game/PriceForge.Game.Infrastructure/PriceForge.Game.Infrastructure.csproj", "src/Modules/Game/PriceForge.Game.Infrastructure/"]
COPY ["src/Modules/Game/PriceForge.Game.Api/PriceForge.Game.Api.csproj", "src/Modules/Game/PriceForge.Game.Api/"]
COPY ["Directory.Packages.props", "./"]
RUN dotnet restore "src/PriceForge.Api/PriceForge.Api.csproj"
COPY . .
WORKDIR "/src/src/PriceForge.Api"
RUN dotnet build "./PriceForge.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PriceForge.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PriceForge.Api.dll"]
