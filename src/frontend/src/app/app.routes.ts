import { Routes } from '@angular/router';
import { MainLayout } from './layout/main-layout/main-layout';

/**
 * Rutas de la aplicación. Cada funcionalidad se carga de forma diferida (lazy loading)
 * desde su carpeta en `features/`. Los módulos de negocio se agregarán aquí.
 */
export const routes: Routes = [
  {
    path: '',
    component: MainLayout,
    children: [
      {
        path: '',
        title: 'Inicio | SocialManagement',
        loadComponent: () => import('./features/home/home-page').then((m) => m.HomePage),
      },
      {
        path: 'estado',
        title: 'Estado del sistema | SocialManagement',
        loadComponent: () =>
          import('./features/system-status/system-status-page').then((m) => m.SystemStatusPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
