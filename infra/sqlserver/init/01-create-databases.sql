/*
  =====================================================================
  SocialManagement - Creación de las bases de datos de los microservicios
  ---------------------------------------------------------------------
  - Idempotente: se puede ejecutar cualquier número de veces.
  - Una base de datos por microservicio (patrón "Database per Service").
  - NO crea tablas: las tablas las crearán las migraciones de EF Core
    de cada servicio cuando existan entidades de negocio.
  =====================================================================
*/
SET NOCOUNT ON;
GO

IF DB_ID(N'SocialManagement_Beneficiarios') IS NULL
BEGIN
    CREATE DATABASE [SocialManagement_Beneficiarios];
    PRINT N'Base de datos SocialManagement_Beneficiarios creada.';
END
ELSE
    PRINT N'Base de datos SocialManagement_Beneficiarios ya existe.';
GO

IF DB_ID(N'SocialManagement_Solicitudes') IS NULL
BEGIN
    CREATE DATABASE [SocialManagement_Solicitudes];
    PRINT N'Base de datos SocialManagement_Solicitudes creada.';
END
ELSE
    PRINT N'Base de datos SocialManagement_Solicitudes ya existe.';
GO

SELECT name, state_desc, create_date
FROM sys.databases
WHERE name LIKE N'SocialManagement[_]%'
ORDER BY name;
GO
