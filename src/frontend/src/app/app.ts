import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Componente raíz: solo aloja el enrutador. El diseño visual está en MainLayout. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {}
