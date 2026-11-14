import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { APP_CONFIG } from '../../core/config/app-config';
import { SystemSnapshot } from './system-status.models';
import { SystemStatusService } from './system-status.service';

/**
 * Pantalla técnica: verifica la disponibilidad del Gateway y, a través de él,
 * de cada microservicio. No contiene lógica de negocio.
 */
@Component({
  selector: 'app-system-status-page',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './system-status-page.html',
  styleUrl: './system-status-page.scss',
})
export class SystemStatusPage {
  private readonly statusService = inject(SystemStatusService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly gatewayBaseUrl = inject(APP_CONFIG).gatewayBaseUrl;
  protected readonly loading = signal(false);
  protected readonly snapshot = signal<SystemSnapshot | null>(null);

  constructor() {
    this.refresh();
  }

  protected refresh(): void {
    this.loading.set(true);
    this.statusService
      .snapshot()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((snapshot) => {
        this.snapshot.set(snapshot);
        this.loading.set(false);
      });
  }
}
