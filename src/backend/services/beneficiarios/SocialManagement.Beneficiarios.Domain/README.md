# SocialManagement.Beneficiarios.Domain

Núcleo del microservicio. No depende de ningún framework, ORM, API ni proveedor externo.

| Carpeta         | Contenido                                                                      |
|-----------------|--------------------------------------------------------------------------------|
| `Entities`      | `Beneficiario`: entidad con identidad (`Id`), invariantes y estado.            |
| `ValueObjects`  | `DocumentoIdentidad`: tipo + número, validado e inmutable, igualdad por valor.  |
| `Enums`         | `TipoDocumentoIdentidad`, `EstadoBeneficiario`.                                |
| `Events`        | Eventos de dominio (aún vacía).                                                |
| `Exceptions`    | `DomainException` y sus derivadas: `BeneficiarioInvalidoException`, `DocumentoIdentidadInvalidoException`, `BeneficiarioDuplicadoException`. |

Reglas de diseño (verificadas en `tests/unit/SocialManagement.Architecture.Tests`):

- Entidades y value objects **sin setters públicos ni constructores públicos**: se crean con métodos de fábrica
  (`Beneficiario.Registrar`, `DocumentoIdentidad.Crear`) que validan las invariantes.
- Toda excepción del dominio hereda de `DomainException` (la API la traduce a HTTP 422).

Explicación didáctica: [docs/sessions/session-02.md](../../../../../docs/sessions/session-02.md).
