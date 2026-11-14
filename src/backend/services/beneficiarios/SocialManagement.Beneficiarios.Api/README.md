# SocialManagement.Beneficiarios.Api

Capa de presentación (ASP.NET Core Web API con Controllers). Depende de `Application` e `Infrastructure`.

| Carpeta         | Contenido                                                                     |
|-----------------|-------------------------------------------------------------------------------|
| `Controllers`   | `BaseApiController`: base de todos los controllers de recursos (ruta versionada `api/v{version:apiVersion}/[controller]`, `ApiResponse<T>`, `UseCase<T>()`, `Logger`). |
| `Endpoints`     | Funciones de Minimal APIs, registradas con `app.MapGet` en `Program.cs`: `GetVersionEndpoint` (`GET /version`) y `GetDemoStatusEndpoint` (`GET /api/demo/status`). |
| `Contracts`     | Modelos HTTP, un tipo por archivo: `ApiResponse<T>`, `ApiError`, `VersionResponse`, `DemoStatusResponse`. |
| `Middleware`    | `GlobalExceptionHandler`: excepciones → ProblemDetails + log.                  |
| `Extensions`    | Registro de servicios de la API, pipeline HTTP y escritura JSON de health checks. |
| `Configuration` | Opciones propias de la API (`ServiceInfoOptions`).                            |

## Endpoints técnicos

| Método | Ruta              | Descripción                                             |
|--------|-------------------|---------------------------------------------------------|
| GET    | `/health`         | Disponibilidad básica (no consulta dependencias).       |
| GET    | `/health/ready`   | Dependencias: SQL Server y RabbitMQ.                     |
| GET    | `/version`        | Nombre, versión, entorno y framework del servicio.       |
| GET    | `/openapi/v1.json`| Documento OpenAPI (Development y Staging; en Production solo con credenciales). |
| GET    | `/swagger`        | Swagger UI. Ver [docs/api-documentation.md](../../../../../docs/api-documentation.md). |
| GET    | `/api/demo/status`| Demostración de la sesión 01: servicio, versión, fecha UTC y estado (200 `Running` / 503 `Stopping`). |

A través del Gateway: `http://localhost:8080/api/beneficiarios/version` y
`http://localhost:8080/api/beneficiarios/api/demo/status`.

Peticiones de ejemplo: [`SocialManagement.Beneficiarios.Api.http`](SocialManagement.Beneficiarios.Api.http).
Explicación didáctica: [docs/sessions/session-01.md](../../../../../docs/sessions/session-01.md).
