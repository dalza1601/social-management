import { InjectionToken, Provider } from '@angular/core';
import { environment } from '../../../environments/environment';

/** Configuración de la aplicación. Su valor depende del entorno (environment*.ts). */
export interface AppConfig {
  readonly production: boolean;
  readonly appName: string;
  /** Prefijo de todas las llamadas al API Gateway (ruta relativa al origen del frontend). */
  readonly gatewayBaseUrl: string;
}

/** Token de inyección: los servicios piden la configuración en lugar de importar environment. */
export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export function provideAppConfig(config: AppConfig = environment): Provider {
  return { provide: APP_CONFIG, useValue: config };
}
