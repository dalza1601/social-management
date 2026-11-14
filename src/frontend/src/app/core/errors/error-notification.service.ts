import { Injectable, signal } from '@angular/core';

/** Estado del aviso global de error que muestra el layout. */
@Injectable({ providedIn: 'root' })
export class ErrorNotificationService {
  private readonly current = signal<string | null>(null);

  readonly message = this.current.asReadonly();

  notify(message: string): void {
    this.current.set(message);
  }

  dismiss(): void {
    this.current.set(null);
  }
}
