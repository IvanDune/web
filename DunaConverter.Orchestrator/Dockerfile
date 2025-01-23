FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DunaConverter.Orchestrator/DunaConverter.Orchestrator.csproj", "DunaConverter.Orchestrator/"]
RUN dotnet restore "DunaConverter.Orchestrator/DunaConverter.Orchestrator.csproj"
COPY . .
WORKDIR "/src/DunaConverter.Orchestrator"
RUN dotnet build "DunaConverter.Orchestrator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DunaConverter.Orchestrator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DunaConverter.Orchestrator.dll"]
