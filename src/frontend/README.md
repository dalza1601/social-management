# Frontend: social-management-web

Aplicación Angular 22 (standalone components, zoneless, Vitest).

```powershell
npm ci                      # dependencias
npm start                   # http://localhost:4200 (proxy /gateway -> http://localhost:8080)
npm run build               # compilación de producción en dist/
npx ng test --watch=false   # pruebas unitarias
```

## Estructura

```text
src/app/
├── core/                    # Servicios transversales (una sola instancia)
│   ├── config/              # APP_CONFIG: configuración por entorno
│   ├── http/                # ApiClient (prefijo del Gateway), ApiError, interceptor de errores
│   └── errors/              # GlobalErrorHandler y aviso global de errores
├── layout/main-layout/      # Encabezado, navegación, aviso de errores y pie de página
└── features/                # Una carpeta por funcionalidad, cargada de forma diferida
    ├── home/                # Bienvenida
    └── system-status/       # Estado del Gateway y de los microservicios
```

Las nuevas funcionalidades se agregan como carpetas en `features/` y como rutas *lazy* en `app.routes.ts`.

## Acceso al Gateway

El código solo conoce el prefijo relativo `/gateway` (`src/environments/`). El destino real se configura fuera del código:

- `npm start`: `proxy.conf.mjs` (variable `GATEWAY_URL`, por defecto `http://localhost:8080`).
- Docker: nginx (variable `GATEWAY_UPSTREAM`, por defecto `http://gateway:8080`).

Más detalles en [docs/local-development.md](../../docs/local-development.md).
