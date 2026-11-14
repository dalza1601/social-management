import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { provideAppConfig } from './core/config/app-config';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppConfig({ production: false, appName: 'SocialManagement (test)', gatewayBaseUrl: '/gateway' }),
      ],
    }).compileComponents();
  });

  it('muestra el layout con la navegación principal', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.brand')?.textContent).toContain('SocialManagement (test)');
    expect(element.textContent).toContain('Estado del sistema');
    expect(element.querySelector('h1')?.textContent).toContain('Gestión de Beneficiarios');
  });
});
