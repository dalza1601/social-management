# SocialManagement.Solicitudes.Application

Orquesta los casos de uso. Solo depende de `Domain`.

| Carpeta        | Qué contendrá                                                                         |
|----------------|---------------------------------------------------------------------------------------|
| `Abstractions` | Contratos técnicos que la aplicación necesita (p. ej. `Messaging/IIntegrationEventPublisher`). |
| `Features`     | Casos de uso organizados por funcionalidad (comandos y consultas).                    |
| `DTOs`         | Objetos de entrada/salida de los casos de uso.                                        |
| `Interfaces`   | Puertos hacia la infraestructura (repositorios, unidad de trabajo, servicios externos). |
| `Behaviors`    | Comportamientos transversales del pipeline (validación, logging, transacciones).      |
| `Validators`   | Validadores FluentValidation de los comandos y consultas (se registran solos en `AddApplication`). |

`DependencyInjection.AddApplication()` es el único punto de registro de esta capa.

> Fase 0: no hay comandos, consultas ni casos de uso. Solo existen los contratos de mensajería.
