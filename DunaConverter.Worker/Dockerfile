FROM mcr.microsoft.com/dotnet/runtime:8.0.12-noble-amd64 AS base
USER root
RUN apt update && apt install -y ffmpeg
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DunaConverter.Worker/DunaConverter.Worker.csproj", "DunaConverter.Worker/"]
RUN dotnet restore "DunaConverter.Worker/DunaConverter.Worker.csproj"
COPY . .
WORKDIR "/src/DunaConverter.Worker"
RUN dotnet build "DunaConverter.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DunaConverter.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DunaConverter.Worker.dll"]
