import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { APP_CONFIG } from '../../core/config/app-config';
import { ErrorNotificationService } from '../../core/errors/error-notification.service';

/** Estructura visual común: encabezado, navegación, aviso de errores y pie de página. */
@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout {
  protected readonly config = inject(APP_CONFIG);
  protected readonly notifications = inject(ErrorNotificationService);
  protected readonly year = new Date().getFullYear();
}
