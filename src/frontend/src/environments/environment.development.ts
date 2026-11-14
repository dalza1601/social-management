import { AppConfig } from '../app/core/config/app-config';

/**
 * Configuración de DESARROLLO (la usa `ng serve`).
 *
 * El servidor de desarrollo reenvía /gateway/* al Gateway según proxy.conf.mjs
 * (por defecto http://localhost:8080, configurable con la variable GATEWAY_URL).
 */
export const environment: AppConfig = {
  production: false,
  appName: 'SocialManagement (dev)',
  gatewayBaseUrl: '/gateway',
};
