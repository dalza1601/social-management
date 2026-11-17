# SocialManagement.Solicitudes.Api

Capa de presentación (ASP.NET Core Web API con Controllers). Depende de `Application` e `Infrastructure`.

| Carpeta         | Contenido                                                                     |
|-----------------|-------------------------------------------------------------------------------|
| `Controllers`   | `BaseApiController`: base de todos los controllers de recursos (ruta versionada `api/v{version:apiVersion}/[controller]`, `ApiResponse<T>`, `UseCase<T>()`, `Logger`). |
| `Endpoints`     | Funciones de Minimal APIs, registradas con `app.MapGet` en `Program.cs`: `GetVersionEndpoint` (`GET /version`). |
| `Contracts`     | Modelos HTTP, un tipo por archivo, agrupados por entidad o funcionalidad: `Diagnostics/` (`VersionResponse`) y `Common/` (`ApiResponse<T>`, `ApiError`). |
| `Middleware`    | `GlobalExceptionHandler`: excepciones → ProblemDetails + log.                  |
| `DependencyInjection` | `DependencyInjection.cs`: `AddPresentation()`: registro de la capa Api (controllers, OpenAPI, errores, AutoMapper, seguridad). |
| `Mappings`      | Perfiles de AutoMapper de la Api: request → comando (se crea con el primer contrato de Solicitudes). |
| `Extensions`    | Un archivo por tema que usa `DependencyInjection/DependencyInjection.cs` (versionado, documentación, seguridad), pipeline HTTP y health checks. |
| `Configuration` | Opciones propias de la API (`ServiceInfoOptions`).                            |

## Endpoints técnicos

| Método | Ruta              | Descripción                                             |
|--------|-------------------|---------------------------------------------------------|
| GET    | `/health`         | Disponibilidad básica (no consulta dependencias).       |
| GET    | `/health/ready`   | Dependencias: SQL Server y RabbitMQ.                     |
| GET    | `/version`        | Nombre, versión, entorno y framework del servicio.       |
| GET    | `/openapi/v1.json`| Documento OpenAPI (Development y Staging; en Production solo con credenciales). |
| GET    | `/swagger`        | Swagger UI. Ver [docs/api-documentation.md](../../../../../docs/api-documentation.md). |

A través del Gateway: `http://localhost:8080/api/solicitudes/version`.
