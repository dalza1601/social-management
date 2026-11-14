import { inject, Injectable } from '@angular/core';
import { catchError, forkJoin, map, Observable, of } from 'rxjs';
import { ApiClient } from '../../core/http/api-client';
import { ApiError } from '../../core/http/api-error';
import {
  CheckResult,
  HealthReport,
  ServiceVersion,
  ServiceVersionCheck,
  SystemSnapshot,
} from './system-status.models';

/** Microservicios publicados por el Gateway (ruta pública: /api/{nombre}/...). */
export const GATEWAY_SERVICES = ['beneficiarios', 'solicitudes'] as const;

/** Consultas técnicas al Gateway. Ninguna comprobación detiene a las demás si falla. */
@Injectable({ providedIn: 'root' })
export class SystemStatusService {
  private readonly api = inject(ApiClient);

  snapshot(): Observable<SystemSnapshot> {
    return forkJoin({
      gateway: this.healthReport('health'),
      downstream: this.healthReport('health/ready'),
      services: forkJoin(GATEWAY_SERVICES.map((name) => this.serviceVersion(name))),
    }).pipe(map((result) => ({ ...result, checkedAt: new Date() })));
  }

  /** Un 503 también trae el informe en el cuerpo: se conserva para mostrar el detalle. */
  private healthReport(path: string): Observable<CheckResult<HealthReport>> {
    return this.api.get<HealthReport>(path).pipe(
      map((data) => ({ ok: data.status === 'Healthy', data, error: null })),
      catchError((error: unknown) =>
        of({
          ok: false,
          data: isHealthReport(error instanceof ApiError ? error.body : null)
            ? ((error as ApiError).body as HealthReport)
            : null,
          error: messageOf(error),
        }),
      ),
    );
  }

  private serviceVersion(name: string): Observable<ServiceVersionCheck> {
    const route = `api/${name}/version`;
    return this.api.get<ServiceVersion>(route).pipe(
      map((data) => ({ name, route: this.api.url(route), result: { ok: true, data, error: null } })),
      catchError((error: unknown) =>
        of({ name, route: this.api.url(route), result: { ok: false, data: null, error: messageOf(error) } }),
      ),
    );
  }
}

function isHealthReport(value: unknown): value is HealthReport {
  return typeof value === 'object' && value !== null && Array.isArray((value as HealthReport).checks);
}

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido.';
}
