# SocialManagement.Beneficiarios.Application

Orquesta los casos de uso. Solo depende de `Domain`.

| Carpeta        | Contenido                                                                              |
|----------------|----------------------------------------------------------------------------------------|
| `Abstractions` | Contratos técnicos (p. ej. `Messaging/IIntegrationEventPublisher`).                    |
| `Features`     | Un caso de uso por carpeta: `RegistrarBeneficiario/` (command + handler) y `ConsultarBeneficiario/` (query + handler). |
| `DTOs`         | `BeneficiarioDto`: datos de salida de los casos de uso.                                |
| `Interfaces`   | Puertos hacia la infraestructura: `IBeneficiarioRepository`.                           |
| `Behaviors`    | Comportamientos transversales del pipeline (aún vacía).                                 |
| `Validators`   | Validaciones de entrada (aún vacía: hoy las invariantes las valida el dominio).        |

`DependencyInjection.AddApplication()` es el único punto de registro de esta capa. Los handlers todavía no se
registran porque no existe una implementación de `IBeneficiarioRepository` (ver el comentario en ese archivo).

Explicación didáctica: [docs/sessions/session-02.md](../../../../../docs/sessions/session-02.md).
