import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppConfig } from '../../core/config/app-config';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import { SystemSnapshot } from './system-status.models';
import { SystemStatusService } from './system-status.service';

describe('SystemStatusService', () => {
  let service: SystemStatusService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        provideAppConfig({ production: false, appName: 'test', gatewayBaseUrl: '/gateway' }),
      ],
    });
    service = TestBed.inject(SystemStatusService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('combina el estado del Gateway, sus dependencias y las versiones de cada servicio', () => {
    let snapshot: SystemSnapshot | undefined;
    service.snapshot().subscribe((value) => (snapshot = value));

    http.expectOne('/gateway/health').flush({ status: 'Healthy', totalDurationMs: 1, checks: [] });
    // Un 503 conserva el detalle de cada dependencia.
    http.expectOne('/gateway/health/ready').flush(
      {
        status: 'Unhealthy',
        totalDurationMs: 5,
        checks: [
          { name: 'beneficiarios', status: 'Healthy', description: 'ok', durationMs: 2, error: null },
          { name: 'solicitudes', status: 'Unhealthy', description: 'sin respuesta', durationMs: 3, error: 'x' },
        ],
      },
      { status: 503, statusText: 'Service Unavailable' },
    );
    http.expectOne('/gateway/api/beneficiarios/version').flush({
      service: 'beneficiarios-api',
      version: '0.1.0',
      environment: 'Development',
      framework: '.NET 10',
      timestampUtc: '2026-01-01T00:00:00Z',
    });
    http.expectOne('/gateway/api/solicitudes/version').flush(null, { status: 502, statusText: 'Bad Gateway' });

    expect(snapshot?.gateway.ok).toBe(true);
    expect(snapshot?.downstream.ok).toBe(false);
    expect(snapshot?.downstream.data?.checks.map((c) => c.status)).toEqual(['Healthy', 'Unhealthy']);
    expect(snapshot?.services.map((s) => s.result.ok)).toEqual([true, false]);
    expect(snapshot?.services[1].result.error).toContain('502');
  });
});
