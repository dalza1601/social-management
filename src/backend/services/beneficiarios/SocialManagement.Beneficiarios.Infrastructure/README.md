# SocialManagement.Beneficiarios.Infrastructure

Implementa los detalles técnicos. Depende de `Application` y `Domain`.

| Carpeta              | Contenido                                                                                  |
|----------------------|--------------------------------------------------------------------------------------------|
| `Persistence`        | `BeneficiariosDbContext` (`DbSet<Beneficiario>`, implementa `IUnitOfWork`). `Configurations/`: mapeo Fluent API por entidad (`BeneficiarioConfiguration`: PK, columnas, índice único, value object; `ConfigureAuditFields()` para la auditoría). `Interceptors/`: `AuditableEntityInterceptor` completa la auditoría en cada `SaveChanges`. Futuro: `Migrations/`. |
| `Configurations`     | Opciones tipadas y validadas: `SqlServerOptions`, `RabbitMqOptions`, `AwsOptions`, `OllamaOptions`. |
| `Repositories`       | Implementaciones de los repositorios definidos en `Application/Interfaces`.                |
| `ExternalServices`   | Clientes de servicios externos. Hoy: `Aws/AwsClientFactory` (S3 y SQS sobre LocalStack).   |
| `Messaging`          | Conexión compartida con RabbitMQ y su health check. Futuro: publicador y consumidores.     |
| `DependencyInjection`| `AddInfrastructure()`: registra todo lo anterior y valida la configuración al iniciar.     |

Reglas:

- El DbContext **no** se comparte con otros microservicios.
- La conexión a RabbitMQ y los clientes AWS se crean de forma perezosa: el servicio arranca
  aunque esas dependencias no estén disponibles, y su estado se ve en `/health/ready`.
