FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BatchWorker/Paperless.BatchWorker.csproj", "BatchWorker/"]
COPY ["Paperless.DAL/Paperless.DAL.csproj", "Paperless.DAL/"]
RUN dotnet restore "./BatchWorker/Paperless.BatchWorker.csproj"
COPY . .
WORKDIR "/src/BatchWorker"
RUN dotnet build "./Paperless.BatchWorker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Paperless.BatchWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Paperless.BatchWorker.dll"]
