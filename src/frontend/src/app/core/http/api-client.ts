import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';

/**
 * Servicio HTTP base. Es el ÚNICO lugar que conoce el prefijo del Gateway:
 * las funcionalidades llaman con rutas relativas, p. ej. `api/beneficiarios/version`.
 */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  get<T>(path: string): Observable<T> {
    return this.http.get<T>(this.url(path));
  }

  /** Construye la URL final: `${gatewayBaseUrl}/${path}` sin barras duplicadas. */
  url(path: string): string {
    const base = this.config.gatewayBaseUrl.replace(/\/+$/, '');
    const relative = path.replace(/^\/+/, '');
    return `${base}/${relative}`;
  }
}
