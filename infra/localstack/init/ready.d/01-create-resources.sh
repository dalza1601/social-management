#!/bin/bash
# =====================================================================
#  Hook "ready" de LocalStack: se ejecuta cuando LocalStack está listo.
#  Crea los recursos AWS locales del proyecto. Es idempotente.
#    - S3  : social-management-documents-dev      (documentos, uso futuro)
#    - SQS : social-management-notifications-dev  (notificaciones, uso futuro)
#  Documentación: https://docs.localstack.cloud/aws/capabilities/config/initialization-hooks/
# =====================================================================
set -euo pipefail

REGION="${AWS_DEFAULT_REGION:-us-east-1}"
BUCKET="${SM_S3_BUCKET:-social-management-documents-dev}"
QUEUE="${SM_SQS_QUEUE:-social-management-notifications-dev}"

# Credenciales ficticias: LocalStack no las valida.
export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"

if command -v awslocal > /dev/null 2>&1; then
  aws_cli() { awslocal --region "$REGION" "$@"; }
elif command -v aws > /dev/null 2>&1; then
  aws_cli() { aws --endpoint-url=http://localhost:4566 --region "$REGION" "$@"; }
else
  echo "[localstack-init] ERROR: no se encontró 'awslocal' ni 'aws' dentro del contenedor." >&2
  exit 1
fi

# ---------- S3 ----------
if aws_cli s3api head-bucket --bucket "$BUCKET" > /dev/null 2>&1; then
  echo "[localstack-init] Bucket s3://$BUCKET ya existe."
else
  if [ "$REGION" = "us-east-1" ]; then
    aws_cli s3api create-bucket --bucket "$BUCKET" > /dev/null
  else
    aws_cli s3api create-bucket --bucket "$BUCKET" \
      --create-bucket-configuration "LocationConstraint=$REGION" > /dev/null
  fi
  echo "[localstack-init] Bucket s3://$BUCKET creado."
fi

# ---------- SQS ----------
# create-queue es idempotente si los atributos no cambian: devuelve la URL existente.
QUEUE_URL=$(aws_cli sqs create-queue --queue-name "$QUEUE" --query QueueUrl --output text)
echo "[localstack-init] Cola SQS lista: $QUEUE_URL"

echo "[localstack-init] Recursos AWS locales aprovisionados."
