import { AppConfig } from '../app/core/config/app-config';

/**
 * Configuración de PRODUCCIÓN (la usa `ng build` y la imagen Docker).
 *
 * `gatewayBaseUrl` es una ruta RELATIVA al propio origen del frontend:
 * nginx (Docker) reenvía /gateway/* al Gateway usando la variable GATEWAY_UPSTREAM.
 * Así el código nunca contiene la dirección real del Gateway.
 */
export const environment: AppConfig = {
  production: true,
  appName: 'SocialManagement',
  gatewayBaseUrl: '/gateway',
};
