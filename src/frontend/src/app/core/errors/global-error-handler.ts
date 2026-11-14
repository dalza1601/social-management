import { ErrorHandler, inject, Injectable } from '@angular/core';
import { ApiError } from '../http/api-error';
import { ErrorNotificationService } from './error-notification.service';

/**
 * Manejo global de errores no controlados: los registra en consola y muestra un aviso
 * genérico al usuario (sin detalles técnicos).
 */
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly notifications = inject(ErrorNotificationService);

  handleError(error: unknown): void {
    console.error('[SocialManagement] Error no controlado:', error);

    const message =
      error instanceof ApiError ? error.message : 'Ocurrió un error inesperado. Intenta nuevamente.';
    this.notifications.notify(message);
  }
}
