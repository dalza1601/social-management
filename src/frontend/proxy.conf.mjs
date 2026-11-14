/**
 * Proxy del servidor de desarrollo (`npm start`).
 *
 * El navegador llama a http://localhost:4200/gateway/... (mismo origen, sin CORS)
 * y el servidor de desarrollo lo reenvía al Gateway eliminando el prefijo /gateway.
 *
 * Destino configurable sin tocar código:
 *   PowerShell:  $env:GATEWAY_URL = "http://localhost:9090"; npm start
 */
const target = process.env['GATEWAY_URL'] ?? 'http://localhost:8080';

export default {
  '/gateway': {
    target,
    secure: false,
    changeOrigin: true,
    pathRewrite: { '^/gateway': '' },
  },
};
