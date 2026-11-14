#!/usr/bin/env bash
# =====================================================================
#  Verifica la documentación (Swagger UI + OpenAPI) de los microservicios.
#
#  Modos:
#    verify-api-docs.sh published <dir-salida>
#        Publica cada API (dotnet publish, igual que el Dockerfile), la ejecuta en Staging
#        y comprueba Swagger UI, sus recursos estáticos y el documento OpenAPI.
#
#    verify-api-docs.sh remote <url-base-del-gateway> <dir-salida>
#        Comprueba un ambiente desplegado a través del Gateway:
#        <url-base>/api/<servicio>/swagger y <url-base>/api/<servicio>/openapi/v1.json
#        Verifica además que el documento anuncie la URL pública (prefijo y esquema correctos).
#
#  Variables opcionales:
#    API_DOCS_USER / API_DOCS_PASSWORD  credenciales HTTP Basic, si la documentación está protegida
#    REDOCLY_CLI_VERSION                versión de @redocly/cli para validar los documentos
#    SKIP_OPENAPI_LINT=1                omite la validación con Redocly (p. ej. sin acceso a npm)
#
#  Los documentos descargados quedan en <dir-salida>/<servicio>.json (útiles como artefacto de CI).
# =====================================================================
set -euo pipefail

MODE="${1:-}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
REDOCLY_CLI_VERSION="${REDOCLY_CLI_VERSION:-2.78.0}"
SERVICES=(beneficiarios solicitudes)

AUTH=()
if [[ -n "${API_DOCS_USER:-}" && -n "${API_DOCS_PASSWORD:-}" ]]; then
  AUTH=(--user "${API_DOCS_USER}:${API_DOCS_PASSWORD}")
fi

log()  { echo "[api-docs] $*"; }
fail() { echo "[api-docs] ERROR: $*" >&2; exit 1; }

pascal() { echo "$(tr '[:lower:]' '[:upper:]' <<< "${1:0:1}")${1:1}"; }

# check_docs <url-base-del-servicio> <url-publica-esperada> <archivo-salida>
check_docs() {
  local base="$1" expected_server="$2" output="$3"

  log "Swagger UI: ${base}/swagger/index.html"
  curl -fsS "${AUTH[@]}" "${base}/swagger/index.html" | grep -q 'swagger-ui' \
    || fail "Swagger UI no responde en ${base}/swagger/index.html"
  curl -fsS "${AUTH[@]}" -o /dev/null "${base}/swagger/swagger-ui-bundle.js" \
    || fail "Faltan los recursos estáticos de Swagger UI en ${base}/swagger/"
  curl -fsS "${AUTH[@]}" "${base}/swagger/index.js" | grep -q '"url":"../openapi/v1.json"' \
    || fail "Swagger UI no apunta al documento relativo ../openapi/v1.json"

  log "OpenAPI: ${base}/openapi/v1.json"
  curl -fsS "${AUTH[@]}" "${base}/openapi/v1.json" -o "${output}" \
    || fail "No se pudo descargar ${base}/openapi/v1.json"

  # El documento debe anunciar la URL pública correcta (esquema, host y prefijo).
  node -e '
    const doc = JSON.parse(require("fs").readFileSync(process.argv[1], "utf8"));
    const expected = process.argv[2].replace(/\/$/, "");
    const servers = (doc.servers || []).map(s => s.url.replace(/\/$/, ""));
    if (!doc.openapi || !doc.info || !doc.info.title) { console.error("Documento sin openapi/info.title"); process.exit(1); }
    if (Object.keys(doc.paths || {}).length === 0) { console.error("El documento no tiene endpoints"); process.exit(1); }
    if (!servers.includes(expected)) { console.error(`servers=${JSON.stringify(servers)}; se esperaba ${expected}`); process.exit(1); }
    console.log(`[api-docs] ${doc.info.title} v${doc.info.version}: ${Object.keys(doc.paths).length} rutas, server ${expected}`);
  ' "${output}" "${expected_server}" || fail "Documento OpenAPI inválido en ${base}"
}

lint_documents() {
  local dir="$1"
  if [[ "${SKIP_OPENAPI_LINT:-0}" == "1" ]]; then
    log "AVISO: validación con Redocly omitida (SKIP_OPENAPI_LINT=1)."
    return 0
  fi
  log "Validando documentos con @redocly/cli@${REDOCLY_CLI_VERSION}"
  npx --yes "@redocly/cli@${REDOCLY_CLI_VERSION}" lint "${dir}"/*.json \
    --config "${REPO_ROOT}/scripts/ci/redocly.yaml" --format stylish
}

WORK_DIR=""
PIDS=()

cleanup() {
  for pid in "${PIDS[@]:-}"; do
    [[ -n "${pid}" ]] && kill "${pid}" 2>/dev/null || true
  done
  [[ -n "${WORK_DIR}" ]] && rm -rf "${WORK_DIR}"
  return 0
}

run_published() {
  local output_dir="$1" port=5180
  WORK_DIR="$(mktemp -d)"
  trap cleanup EXIT

  for service in "${SERVICES[@]}"; do
    local name; name="$(pascal "${service}")"
    local project="${REPO_ROOT}/src/backend/services/${service}/SocialManagement.${name}.Api/SocialManagement.${name}.Api.csproj"
    local publish_dir="${WORK_DIR}/${service}"
    port=$((port + 1))

    log "Publicando ${service} (Release)"
    dotnet publish "${project}" --configuration Release --output "${publish_dir}" -p:UseAppHost=false --verbosity quiet

    log "Iniciando ${service} en http://127.0.0.1:${port} (Staging)"
    # Configuración mínima para arrancar: las dependencias (SQL Server, RabbitMQ) no se contactan al iniciar.
    # "export" admite un nombre de variable dinámico (ConnectionStrings__BeneficiariosDb, ...SolicitudesDb).
    # "exec" hace que el PID registrado sea el de dotnet, para poder detenerlo al terminar.
    ( cd "${publish_dir}"
      export ASPNETCORE_ENVIRONMENT=Staging
      export ASPNETCORE_URLS="http://127.0.0.1:${port}"
      export "ConnectionStrings__${name}Db=Server=127.0.0.1,1;Database=ci;User Id=ci;Password=ci;Connect Timeout=1"
      export RabbitMq__Host=127.0.0.1 RabbitMq__UserName=ci RabbitMq__Password=ci
      exec dotnet "SocialManagement.${name}.Api.dll" > "${output_dir}/${service}.log" 2>&1 ) &
    PIDS+=("$!")

    local ready=false
    for _ in $(seq 1 60); do
      if curl -fs -o /dev/null "http://127.0.0.1:${port}/health"; then ready=true; break; fi
      sleep 1
    done
    [[ "${ready}" == true ]] || { cat "${output_dir}/${service}.log" >&2; fail "${service} no inició"; }

    check_docs "http://127.0.0.1:${port}" "http://127.0.0.1:${port}" "${output_dir}/${service}.json"
  done

  lint_documents "${output_dir}"
}

run_remote() {
  local base_url="${1%/}" output_dir="$2"
  [[ "${base_url}" =~ ^https?:// ]] || fail "La URL base debe empezar por http:// o https://"
  [[ "${base_url}" == https://* ]] || log "AVISO: ${base_url} no usa HTTPS (aceptable solo en entornos locales)."

  for service in "${SERVICES[@]}"; do
    check_docs "${base_url}/api/${service}" "${base_url}/api/${service}" "${output_dir}/${service}.json"
  done

  lint_documents "${output_dir}"
}

case "${MODE}" in
  published)
    [[ $# -eq 2 ]] || fail "Uso: $0 published <dir-salida>"
    mkdir -p "$2"; OUT="$(cd "$2" && pwd)"
    run_published "${OUT}" ;;
  remote)
    [[ $# -eq 3 ]] || fail "Uso: $0 remote <url-base> <dir-salida>"
    mkdir -p "$3"; OUT="$(cd "$3" && pwd)"
    run_remote "$2" "${OUT}" ;;
  *)
    fail "Uso: $0 published <dir-salida> | remote <url-base> <dir-salida>" ;;
esac

log "Documentación verificada. Documentos en ${OUT}"
