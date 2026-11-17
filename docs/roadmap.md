# Hoja de ruta

La **Fase 0** entrega la base técnica. Los módulos siguientes construyen sobre ella.

> Esta es una propuesta de secuencia. Ajusta el orden y el alcance al sílabo oficial del curso.

## Fase 0: Foundation ✅

- Monorepositorio, convenciones y versiones centralizadas.
- Dos microservicios con Clean Architecture (Domain, Application, Infrastructure, Api).
- API Gateway con YARP, frontend Angular base y pantalla de estado.
- SQL Server (dos bases), RabbitMQ, LocalStack (S3/SQS) y Ollama en Docker Compose.
- Health checks, ProblemDetails, OpenAPI, logging estructurado y validación de configuración.
- Pruebas de arquitectura, integración y entorno; scripts PowerShell; CI en GitHub Actions.

## Módulo 1: Dominio y persistencia: Beneficiarios

- ✅ Entidades, objetos de valor y reglas del dominio de Beneficiarios ([sesión 02](sessions/session-02.md)).
- ✅ Configuraciones de EF Core, primera migración y repositorios ([sesión 03](sessions/session-03.md)).
- ✅ Endpoints REST y DTOs; validación de entrada ([sesión 03](sessions/session-03.md)).
- Pantallas Angular de consulta y registro.

## Módulo 2: Solicitudes y CQRS

- Dominio de Solicitudes (estados, transiciones, reglas).
- Separación de comandos y consultas; *pipeline behaviors* (validación, logging, transacciones).
- Paginación, filtros y manejo de concurrencia.

## Módulo 3: Comunicación entre microservicios

- ✅ Diseño: contextos, contratos iniciales (eventos v1 y consulta HTTP) y propiedad de datos ([sesión 04](sessions/session-04.md), [documento](microservices-design.md)).
- Publicador y consumidores RabbitMQ (implementaciones de `IIntegrationEventPublisher` / `IIntegrationEventHandler<T>`).
- Eventos de integración entre Beneficiarios y Solicitudes; idempotencia y reintentos.
- Patrón *Outbox* y orquestación o coreografía (*Saga*).
- Notificaciones con SQS sobre LocalStack.

## Módulo 4: Seguridad

- ✅ Autenticación JWT con roles y políticas en Beneficiarios; tokens solo de desarrollo ([sesión 04](sessions/session-04.md)).
- Proveedor de identidad con OAuth 2.0 y OpenID Connect; *refresh tokens*.
- Autorización en el Gateway y en los servicios.
- Login y guardas de rutas en Angular.
- Gestión de secretos fuera de `.env`.

## Módulo 5: IA generativa

- Integración con Semantic Kernel usando Ollama (local) como proveedor.
- Casos de uso asistidos por IA sobre solicitudes (clasificación, resúmenes).
- Embeddings y RAG sobre documentos almacenados en S3 (LocalStack).

## Módulo 6: DevOps y observabilidad

- Pipeline de CI/CD completo (versionado, imágenes, análisis de calidad y seguridad).
- Observabilidad: OpenTelemetry, trazas distribuidas y métricas.
- Estrategia de despliegue a la nube y gestión de configuración por entorno.

## Fuera del alcance de la Fase 0

CRUD, entidades y reglas de negocio · autenticación · CQRS / MediatR · Saga · eventos de negocio ·
formularios · integración funcional de IA · RAG · agentes · despliegues en AWS real · Kubernetes.
