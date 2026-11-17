# Solución de problemas

## Scripts de PowerShell

**"No se puede cargar el archivo ... porque la ejecución de scripts está deshabilitada"**

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
# o, solo para una ejecución:
powershell -ExecutionPolicy Bypass -File .\scripts\setup.ps1
```

**"No existe el archivo .env"**: ejecuta `.\scripts\setup.ps1`.

## Docker

**"Docker está instalado, pero el motor no responde"**: abre Docker Desktop y espera a "Engine running".
Si usas WSL2, ejecuta `wsl --update` y reinicia Docker Desktop.

**Un puerto ya está en uso** (`bind: address already in use`): cambia el puerto en `.env`
(p. ej. `GATEWAY_PORT=9080`) y vuelve a ejecutar `start.ps1`. Para ver qué proceso lo usa:

```powershell
Get-NetTCPConnection -LocalPort 8080 -State Listen | Select-Object OwningProcess
Get-Process -Id <OwningProcess>
```

**Un servicio queda `unhealthy` o `start.ps1` agota el tiempo de espera**

```powershell
docker compose -f infra/docker/docker-compose.yml --env-file .env ps -a
docker compose -f infra/docker/docker-compose.yml --env-file .env logs <servicio>
```

La primera ejecución descarga unos 3 GB de imágenes: en conexiones lentas usa `.\scripts\start.ps1 -TimeoutSeconds 1200`.

## SQL Server

**El contenedor `sqlserver` se reinicia constantemente**

- La contraseña no cumple la política: mínimo 8 caracteres con mayúsculas, minúsculas, números y símbolos.
  `setup.ps1` lo valida.
- Docker Desktop tiene menos de 2 GB de RAM disponibles: aumenta la memoria en *Settings → Resources*.
- **Cambiaste `MSSQL_SA_PASSWORD` después del primer arranque**: la contraseña real quedó guardada en el volumen.
  Restaura el valor anterior o elimina los datos con `.\scripts\reset.ps1`.

**`sqlserver-init` termina con código distinto de 0**: revisa `docker compose ... logs sqlserver-init`.

**Las APIs fallan en `/health/ready` con "Login failed for user 'sa'"**: la contraseña de `.env` no coincide con la del
volumen (ver el punto anterior). Con `dotnet run`, vuelve a ejecutar `setup.ps1` para actualizar user-secrets.

## RabbitMQ

**La consola no acepta `guest/guest`**: es intencional. Usa `RABBITMQ_USER` / `RABBITMQ_PASSWORD` de `.env`.

**Cambié el usuario o la contraseña y ya no conecta**: RabbitMQ solo crea el usuario por defecto la primera vez.
Usa `.\scripts\reset.ps1` o crea el usuario desde la consola de administración.

## APIs y configuración

**El servicio no arranca: "Falta la configuración obligatoria '...'"**: el mensaje indica la clave y la variable de
entorno equivalente. Con `dotnet run`, ejecuta `.\scripts\setup.ps1` (configura user-secrets). En Docker, revisa `.env`.

**El servicio no arranca: "Falta la configuración obligatoria 'Jwt:SigningKey'" o "debe tener al menos 32 bytes"**: falta la
clave de firma de los tokens (sesión 04). Ejecuta `.\scripts\setup.ps1`: agrega `JWT_SIGNING_KEY` a tu `.env` si no la tenía y
la guarda en user-secrets. En Docker llega como `Jwt__SigningKey` desde `.env`.

**El servicio no arranca: "'DevelopmentTokens:Enabled' solo puede ser true en el ambiente Development"**: alguien activó la
emisión de tokens de demostración en Staging o Production. Quítala (`DevelopmentTokens__Enabled=false`).

**El servicio no arranca en Development por `DevelopmentTokens:Users`**: faltan los usuarios demo o están mal configurados
(clave de menos de 12 caracteres, rol distinto de `Administrador`/`Operador`, nombres repetidos). Ejecuta `.\scripts\setup.ps1`.

**`/api/v1/beneficiarios` responde 401**: falta el token, expiró (dura 60 minutos) o se firmó con otra clave (p. ej.
lo pediste a otra instancia). La cabecera `WWW-Authenticate` explica el motivo. Pide uno nuevo en `POST /api/auth/dev-token`;
en Swagger, pega solo el token en **Authorize**, sin la palabra `Bearer`.

**Responde 403**: el token es válido pero tu rol no permite esa operación (p. ej. el Operador no puede desactivar).
Ver la matriz de permisos en [security.md](security.md#roles-políticas-y-permisos).

**`POST /api/auth/dev-token` responde 404**: solo existe en el ambiente Development.

**`/health/ready` devuelve 503 pero `/health` devuelve 200**: el servicio funciona, pero no alcanza SQL Server o RabbitMQ.
El JSON de respuesta indica cuál falla y por qué.

**Error `MSB3030: No se pudo copiar el archivo ...` al compilar en Windows**: la ruta del repositorio es demasiado
larga (límite de 260 caracteres). Clona en una ruta corta como `C:\dev\social-management` o habilita las rutas largas
(como administrador):

```powershell
New-ItemProperty -Path HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem -Name LongPathsEnabled -Value 1 -PropertyType DWORD -Force
```

**`dotnet test` muestra "Testing with VSTest target is no longer supported"**: ejecuta los comandos desde la raíz del
repositorio, donde `global.json` activa Microsoft.Testing.Platform, y usa `dotnet test --solution ...` o `--project ...`.

## Swagger / OpenAPI

**`/swagger` devuelve 404**: la documentación está deshabilitada en ese ambiente. Revisa `ApiDocumentation__Enabled`
(en Docker: `API_DOCS_ENABLED` en `.env`). En `Production` está deshabilitada por defecto.

**El servicio no arranca en Production: "la documentación de la API solo puede habilitarse con control de acceso"**:
define `ApiDocumentation__Access__UserName` y `ApiDocumentation__Access__Password` (mínimo 12 caracteres) o deshabilítala.

**El navegador pide usuario y contraseña en `/swagger`**: hay credenciales configuradas (`API_DOCS_USER`/`API_DOCS_PASSWORD`).

**Detrás del Gateway, "Try it out" llama a `http://beneficiarios-api:8080` o pierde el prefijo `/api/...`**: la API no
confía en el proxy. Agrega su red o IP en `ForwardedHeaders__KnownNetworks__0` / `ForwardedHeaders__KnownProxies__0`,
y verifica que el proxy envíe `X-Forwarded-Prefix`, `X-Forwarded-Host` y `X-Forwarded-Proto`.
Detalle en [api-documentation.md](api-documentation.md).

## Gateway y frontend

**`/api/beneficiarios/...` devuelve 502 Bad Gateway**: el Gateway no alcanza el servicio. Comprueba
`http://localhost:8080/health/ready`. Si ejecutas el Gateway con `dotnet run`, los servicios deben estar en
`localhost:8081` y `localhost:8082`.

**El frontend muestra "No fue posible contactar con el servidor"**

- Con `npm start`: el Gateway debe estar en `http://localhost:8080` (o define `GATEWAY_URL`).
- En Docker: revisa `docker compose ... logs frontend` y que el servicio `gateway` esté `healthy`.

**Recordatorio:** dentro de un contenedor, `localhost` es el propio contenedor. Entre contenedores usa el nombre del
servicio y su puerto interno (`http://gateway:8080`).

## LocalStack

**`start-full.ps1` dice que LocalStack no se iniciará**: `LOCALSTACK_AUTH_TOKEN` está vacío en `.env`.
Ver [infrastructure.md](infrastructure.md#localstack-perfil-opcional-localstack).

**El contenedor `localstack` se detiene al arrancar**: el token no es válido, expiró o tu plan no permite la imagen.

```powershell
docker compose -f infra/docker/docker-compose.yml --env-file .env --profile localstack logs localstack
```

Busca mensajes de licencia o activación. Con el contenedor activo, <http://localhost:4566/_localstack/info> debe mostrar
`"is_license_activated": true`. El resto del entorno sigue funcionando sin LocalStack.

**El bucket o la cola no existen**: revisa la salida del hook en los logs (`[localstack-init]`).
Si editaste el script en Windows, comprueba que use finales de línea LF (`.gitattributes` lo fuerza al clonar).

## Ollama

**`ollama list` no muestra modelos**: es lo esperado; descárgalos manualmente (ver [infrastructure.md](infrastructure.md#ollama-perfil-opcional-ollama)).

**La descarga o la ejecución del modelo es muy lenta**: elige un modelo más pequeño (`qwen2.5:0.5b`, `llama3.2:1b`)
o aumenta la memoria de Docker Desktop.
