#!/usr/bin/env bash
# =====================================================================
#  Ejecutado por el contenedor "sqlserver-init" (Docker Compose).
#  Espera a que SQL Server acepte conexiones y ejecuta los scripts de
#  init/ en orden alfabético. Todos los scripts deben ser idempotentes.
# =====================================================================
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
HOST="${SQLSERVER_HOST:-sqlserver}"
: "${MSSQL_SA_PASSWORD:?La variable MSSQL_SA_PASSWORD es obligatoria}"

echo "[sqlserver-init] Esperando a SQL Server en ${HOST}..."
for attempt in $(seq 1 30); do
  if "$SQLCMD" -S "$HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "SELECT 1" -o /dev/null; then
    break
  fi
  if [ "$attempt" -eq 30 ]; then
    echo "[sqlserver-init] ERROR: SQL Server no respondió después de 30 intentos." >&2
    exit 1
  fi
  sleep 2
done

for script in /scripts/init/*.sql; do
  echo "[sqlserver-init] Ejecutando $(basename "$script")"
  "$SQLCMD" -S "$HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -b -i "$script"
done

echo "[sqlserver-init] Inicialización completada."
