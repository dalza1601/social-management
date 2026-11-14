# Desarrollo local

## 1. Instalar requisitos

| Herramienta | Versión | Descarga |
|-------------|---------|----------|
| Docker Desktop (con WSL2) | Reciente | <https://www.docker.com/products/docker-desktop/> |
| .NET SDK | 10.0.400 o superior en la rama 10 | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| Node.js | 24 LTS (mínimo 22.12) | <https://nodejs.org/> |
| Git | Reciente | <https://git-scm.com/> |
| Editor | Visual Studio 2026, VS Code o Rider | — |

Docker Desktop necesita al menos **8 GB de RAM** asignados (SQL Server usa unos 2 GB).

## 2. Primera ejecución

```powershell
git clone <url-del-repositorio> C:\dev\social-management   # usa una ruta corta (ver troubleshooting)
cd C:\dev\social-management

Set-ExecutionPolicy -Scope CurrentUser RemoteSigned          # solo si Windows bloquea los scripts
.\scripts\setup.ps1
.\scripts\start.ps1
.\scripts\check.ps1
```

`setup.ps1`:

1. Comprueba Docker, Docker Compose, .NET 10, Node.js y npm.
2. Crea `.env` desde `.env.example` (nunca sobrescribe uno existente).
3. Valida la contraseña de SQL Server y avisa si un puerto está ocupado.
4. Guarda en **user-secrets** la cadena de conexión y las credenciales de RabbitMQ para `dotnet run`.
5. Restaura NuGet e instala las dependencias del frontend.

## 3. Modos de trabajo

### A. Todo en Docker (recomendado para empezar)

```powershell
.\scripts\start.ps1
```

Abre <http://localhost:4200>.

### B. Infraestructura en Docker y código en el IDE (para depurar)

1. Levanta el entorno: `.\scripts\start.ps1`.
2. Detén el contenedor del servicio que vas a depurar (libera su puerto):

   ```powershell
   docker compose -f infra/docker/docker-compose.yml --env-file .env stop beneficiarios-api
   ```

3. Ejecútalo desde el IDE o la terminal (usa `appsettings.Development.json` + user-secrets y escucha en el mismo puerto 8081):

   ```powershell
   dotnet run --project src/backend/services/beneficiarios/SocialManagement.Beneficiarios.Api
   ```

   El Gateway en Docker **no** lo verá (apunta a `beneficiarios-api:8080`). Para probarlo por el Gateway, ejecuta
   también el Gateway localmente (paso 4).

4. Gateway local (sus destinos por defecto son `http://localhost:8081/` y `http://localhost:8082/`):

   ```powershell
   docker compose -f infra/docker/docker-compose.yml --env-file .env stop gateway frontend
   dotnet run --project src/backend/gateway/SocialManagement.Gateway
   ```

5. Frontend local con recarga en caliente:

   ```powershell
   cd src\frontend
   npm start          # http://localhost:4200
   ```

| Proyecto | URL con `dotnet run` / `npm start` |
|----------|------------------------------------|
| Beneficiarios API | <http://localhost:8081> |
| Solicitudes API | <http://localhost:8082> |
| Gateway | <http://localhost:8080> |
| Frontend | <http://localhost:4200> |

### Cómo llega el frontend al Gateway (sin URLs en el código)

El código Angular solo conoce el prefijo relativo `/gateway` (`src/environments/environment*.ts`).
`ApiClient` lo antepone a cada ruta, y quien está delante reenvía la petición:

| Modo | Quién reenvía `/gateway/*` | Dónde se configura el destino |
|------|----------------------------|-------------------------------|
| `npm start` | Servidor de desarrollo de Angular | `src/frontend/proxy.conf.mjs` (variable `GATEWAY_URL`, por defecto `http://localhost:8080`) |
| Docker | nginx del contenedor `frontend` | Variable `GATEWAY_UPSTREAM` (por defecto `http://gateway:8080`) |

Como el navegador siempre llama a su mismo origen, **no hace falta CORS**.

```powershell
# Ejemplo: Gateway en otro puerto durante el desarrollo
$env:GATEWAY_URL = "http://localhost:9090"; npm start
```

## 4. Compilar y probar

```powershell
dotnet build SocialManagement.slnx                       # toda la solución
dotnet build src/backend/services/beneficiarios/SocialManagement.Beneficiarios.slnx   # un solo servicio

dotnet test --solution SocialManagement.slnx             # todas las pruebas
dotnet test --project tests/unit/SocialManagement.Architecture.Tests
```

| Proyecto de pruebas | Tipo | Necesita Docker |
|---------------------|------|-----------------|
| `tests/unit/SocialManagement.Architecture.Tests` | Unitarias: reglas de Clean Architecture | No |
| `tests/unit/SocialManagement.Beneficiarios.Api.UnitTests` | Unitarias: controllers de la API (sesión 01) | No |
| `tests/unit/SocialManagement.Beneficiarios.Domain.UnitTests` | Unitarias: invariantes del dominio (sesión 02) | No |
| `tests/unit/SocialManagement.Beneficiarios.Application.UnitTests` | Unitarias: casos de uso con dobles de prueba (sesión 02) | No |
| `tests/unit/SocialManagement.Beneficiarios.Infrastructure.UnitTests` | Unitarias: mapeo EF Core (`BeneficiarioConfiguration`) y script SQL, sin base de datos | No |
| `tests/integration/SocialManagement.*.Api.IntegrationTests` | Integración en memoria: arranque, DI, endpoints, errores, validación de configuración | No |
| `tests/integration/SocialManagement.Gateway.IntegrationTests` | Integración: rutas YARP contra servicios simulados | No |
| `tests/environment/SocialManagement.LocalEnvironment.Tests` | Validación estática de `docker-compose.yml` (siempre) + contenedores en vivo (opcional) | Solo las pruebas en vivo |

Las pruebas en vivo se **omiten** (aparecen como *skipped*) salvo que se activen:

```powershell
.\scripts\check.ps1 -RunTests          # las activa según lo que esté en ejecución

# o manualmente
$env:SM_ENVIRONMENT_TESTS = "true"     # SQL Server, RabbitMQ, APIs, Gateway, frontend
$env:SM_LOCALSTACK_TESTS  = "true"     # bucket S3 y cola SQS
$env:SM_OLLAMA_TESTS      = "true"     # servidor Ollama
dotnet test --project tests/environment/SocialManagement.LocalEnvironment.Tests
```

Frontend:

```powershell
cd src\frontend
npm run build
npx ng test --watch=false
```

## 5. Detener, reiniciar y limpiar

```powershell
.\scripts\stop.ps1             # detiene todo; conserva los datos
.\scripts\restart.ps1          # stop + start
.\scripts\restart.ps1 -Full    # incluye LocalStack y Ollama
.\scripts\reset.ps1            # BORRA volúmenes (pide escribir BORRAR)
```

## 6. Convenciones

- Código y nombres técnicos en inglés; documentación, mensajes al usuario y nombres de dominio en español.
- `.editorconfig` define el estilo (namespaces de archivo, `_camelCase` para campos privados, LF).
- Versiones de paquetes solo en `Directory.Packages.props`.
- **Un tipo por archivo**, con el mismo nombre que el tipo (verificado por `SourceFileConventionsTests`). Los modelos
  HTTP van en `Api/Contracts/`; los endpoints puntuales se escriben como Minimal APIs (`Api/Endpoints/` + `app.MapGet`)
  y los recursos con varias operaciones como controllers que heredan de `BaseApiController`
  (ver [architecture.md](architecture.md#convenciones-de-la-capa-api)).
- En CI, cualquier advertencia de compilación es un error (`TreatWarningsAsErrors` cuando `CI=true`).
- Nunca subas `.env` ni secretos. Usa user-secrets o variables de entorno.
