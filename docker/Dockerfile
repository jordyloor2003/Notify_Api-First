# ==============================================================================
# Multi-Stage Dockerfile para Notify API (.NET 8 LTS)
# Diseñado para máxima portabilidad, seguridad (Alpine) y peso reducido (<120MB)
# ==============================================================================

# STAGE 1: Compilación y Publicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar archivos de proyecto y restaurar dependencias (optimiza caché de Docker)
COPY NotifyApi.sln* ./
COPY src/NotifyApi.Domain/NotifyApi.Domain.csproj src/NotifyApi.Domain/
COPY src/NotifyApi.Application/NotifyApi.Application.csproj src/NotifyApi.Application/
COPY src/NotifyApi.Infrastructure/NotifyApi.Infrastructure.csproj src/NotifyApi.Infrastructure/
COPY src/NotifyApi.WebApi/NotifyApi.WebApi.csproj src/NotifyApi.WebApi/
COPY src/NotifyApi.Worker/NotifyApi.Worker.csproj src/NotifyApi.Worker/
COPY tests/NotifyApi.UnitTests/NotifyApi.UnitTests.csproj tests/NotifyApi.UnitTests/

RUN dotnet restore src/NotifyApi.WebApi/NotifyApi.WebApi.csproj

# Copiar todo el código fuente y publicar
COPY . .
WORKDIR /app/src/NotifyApi.WebApi
RUN dotnet publish -c Release -o /app/publish --no-restore

# STAGE 2: Imagen de Runtime Ligera y Segura
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Crear usuario sin privilegios root por buenas prácticas de seguridad (DevSecOps)
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "NotifyApi.WebApi.dll"]
