# SocialManagement.Beneficiarios.Application

Orquesta los casos de uso. Solo depende de `Domain`.

| Carpeta        | Contenido                                                                              |
|----------------|----------------------------------------------------------------------------------------|
| `Abstractions` | Contratos técnicos (p. ej. `Messaging/IIntegrationEventPublisher`).                    |
| `Common`       | Piezas comunes a todos los casos de uso: `Pagination/PaginationRequest` y `Pagination/PagedResponse<T>`. |
| `Features`     | Un caso de uso por carpeta (command o query + handler): registrar, consultar, listar (con `BeneficiarioQueryParameters`: página, búsqueda y orden), actualizar y desactivar. |
| `DTOs`         | `BeneficiarioDto`: datos de salida de los casos de uso.                                |
| `Mappings`     | Perfiles de AutoMapper: `BeneficiarioProfile` (entidad → `BeneficiarioDto`). |
| `Interfaces`   | Puertos hacia la infraestructura: `IBeneficiarioRepository` y `IUnitOfWork` (lo usan los handlers para confirmar). |
| `Behaviors`    | Comportamientos transversales del pipeline (aún vacía).                                 |
| `Validators`   | Validadores FluentValidation de los comandos y consultas (`RegistrarBeneficiarioCommandValidator`, `ConsultarBeneficiarioQueryValidator`). Reutilizan las reglas del dominio. |

`DependencyInjection/DependencyInjection.cs` (`AddApplication()`) es el único punto de registro de esta capa y lo registra todo por
convención: validadores, perfiles de AutoMapper y cada clase `*Handler` de `Features`. Un caso de uso nuevo no requiere
tocar ese archivo.

Explicación didáctica: [docs/sessions/session-02.md](../../../../../docs/sessions/session-02.md).
