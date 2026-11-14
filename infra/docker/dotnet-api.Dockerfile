# syntax=docker/dockerfile:1
# =====================================================================
#  Dockerfile multi-stage reutilizable para las aplicaciones ASP.NET Core
#  (Beneficiarios API, Solicitudes API y Gateway).
#
#  Contexto de construcción: raíz del repositorio.
#  Build args:
#    PROJECT_DIR  - carpeta del proyecto, relativa a la raíz del repositorio
#    PROJECT_NAME - nombre del .csproj sin extensión (= nombre del ensamblado)
# =====================================================================
ARG DOTNET_VERSION=10.0

# ---------- Etapa 1: compilación y publicación ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG PROJECT_DIR
ARG PROJECT_NAME
WORKDIR /src

# Configuración común (versiones centralizadas de paquetes).
COPY Directory.Build.props Directory.Packages.props ./
# Código fuente del backend (bin/obj se excluyen en .dockerignore).
COPY src/backend/ src/backend/

RUN dotnet restore "${PROJECT_DIR}/${PROJECT_NAME}.csproj"
RUN dotnet publish "${PROJECT_DIR}/${PROJECT_NAME}.csproj" \
      --configuration Release \
      --no-restore \
      --output /app/publish \
      -p:UseAppHost=false

# ---------- Etapa 2: imagen final mínima ----------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
ARG PROJECT_NAME

# curl solo se usa para el HEALTHCHECK del contenedor.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
# Los archivos quedan como propiedad de root: el usuario de ejecución solo puede leerlos.
COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    APP_DLL=${PROJECT_NAME}.dll

EXPOSE 8080

# Usuario sin privilegios incluido en las imágenes oficiales de .NET (UID 1654).
USER $APP_UID

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
  CMD curl -fsS http://localhost:8080/health > /dev/null || exit 1

ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\""]
