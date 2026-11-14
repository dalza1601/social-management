import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppConfig } from '../config/app-config';
import { ApiClient } from './api-client';
import { ApiError } from './api-error';
import { httpErrorInterceptor } from './http-error.interceptor';

describe('ApiClient', () => {
  let client: ApiClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        provideAppConfig({ production: false, appName: 'test', gatewayBaseUrl: '/gateway/' }),
      ],
    });
    client = TestBed.inject(ApiClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('antepone el prefijo del Gateway sin duplicar barras', () => {
    expect(client.url('/api/beneficiarios/version')).toBe('/gateway/api/beneficiarios/version');
    expect(client.url('health')).toBe('/gateway/health');
  });

  it('envía las peticiones a través del Gateway', () => {
    let response: unknown;
    client.get<{ status: string }>('health').subscribe((value) => (response = value));

    http.expectOne('/gateway/health').flush({ status: 'Healthy' });

    expect(response).toEqual({ status: 'Healthy' });
  });

  it('convierte los errores HTTP en ApiError con un mensaje comprensible', () => {
    let captured: unknown;
    client.get('api/solicitudes/version').subscribe({ error: (error) => (captured = error) });

    http
      .expectOne('/gateway/api/solicitudes/version')
      .flush({ title: 'Bad Gateway', detail: 'Servicio no disponible' }, { status: 502, statusText: 'Bad Gateway' });

    expect(captured).toBeInstanceOf(ApiError);
    expect((captured as ApiError).status).toBe(502);
    expect((captured as ApiError).message).toContain('Servicio no disponible');
  });
});
