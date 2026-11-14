/** Formato JSON de los health checks del Gateway y de las APIs. */
export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

export interface HealthCheckEntry {
  name: string;
  status: HealthStatus;
  description: string | null;
  durationMs: number;
  error: string | null;
}

export interface HealthReport {
  status: HealthStatus;
  totalDurationMs: number;
  checks: HealthCheckEntry[];
}

/** Respuesta de GET /version de cada microservicio. */
export interface ServiceVersion {
  service: string;
  version: string;
  environment: string;
  framework: string;
  timestampUtc: string;
}

/** Resultado de una comprobación: datos si respondió, mensaje si falló. */
export interface CheckResult<T> {
  ok: boolean;
  data: T | null;
  error: string | null;
}

export interface ServiceVersionCheck {
  name: string;
  route: string;
  result: CheckResult<ServiceVersion>;
}

export interface SystemSnapshot {
  checkedAt: Date;
  gateway: CheckResult<HealthReport>;
  downstream: CheckResult<HealthReport>;
  services: ServiceVersionCheck[];
}
