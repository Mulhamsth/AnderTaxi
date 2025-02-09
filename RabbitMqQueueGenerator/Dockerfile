FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RabbitMqQueueGenerator/RabbitMqQueueGenerator.csproj", "RabbitMqQueueGenerator/"]
RUN dotnet restore "RabbitMqQueueGenerator/RabbitMqQueueGenerator.csproj"
COPY . .
WORKDIR "/src/RabbitMqQueueGenerator"
RUN dotnet build "RabbitMqQueueGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RabbitMqQueueGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RabbitMqQueueGenerator.dll"]
