# SocialManagement.Solicitudes.Domain

Núcleo del microservicio. No depende de ningún framework, ORM, API ni proveedor externo.

| Carpeta         | Qué contendrá                                                                  |
|-----------------|--------------------------------------------------------------------------------|
| `Entities`      | Entidades y agregados con identidad propia.                                    |
| `ValueObjects`  | Objetos inmutables comparados por valor (documento de identidad, dirección…).  |
| `Enums`         | Enumeraciones del dominio.                                                     |
| `Events`        | Eventos de dominio que expresan hechos ocurridos dentro del agregado.          |
| `Exceptions`    | Excepciones de reglas de dominio. Todas heredan de `DomainException`.          |

> Fase 0: las carpetas están vacías a propósito (`.gitkeep`). Las entidades se crean en el módulo 1.
