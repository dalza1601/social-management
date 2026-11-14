# syntax=docker/dockerfile:1
# =====================================================================
#  Frontend Angular: compilación con Node y publicación con nginx sin privilegios.
#  Contexto de construcción: raíz del repositorio.
# =====================================================================

# ---------- Etapa 1: compilación ----------
FROM node:24-alpine AS build
WORKDIR /app

# Primero solo los manifiestos: la capa de dependencias se reutiliza si no cambian.
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY src/frontend/ ./
RUN npm run build

# ---------- Etapa 2: servidor web ----------
FROM nginxinc/nginx-unprivileged:1.30-alpine AS runtime

# La plantilla se procesa con envsubst al iniciar el contenedor y genera
# /etc/nginx/conf.d/default.conf con la dirección real del Gateway.
COPY infra/docker/frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/social-management-web/browser /usr/share/nginx/html

# Dirección INTERNA del Gateway (DNS de Docker Compose). Se puede sobrescribir.
ENV GATEWAY_UPSTREAM=http://gateway:8080

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=5 \
  CMD wget -q --spider http://127.0.0.1:8080/ || exit 1
