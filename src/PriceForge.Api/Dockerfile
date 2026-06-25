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
