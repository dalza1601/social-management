import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Pantalla de bienvenida. No contiene funcionalidades de negocio. */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink],
  templateUrl: './home-page.html',
})
export class HomePage {
  protected readonly components = [
    { name: 'API Gateway (YARP)', detail: 'Punto de entrada único: /api/beneficiarios y /api/solicitudes.' },
    { name: 'Beneficiarios API', detail: 'Microservicio con Clean Architecture y base de datos propia.' },
    { name: 'Solicitudes API', detail: 'Microservicio con Clean Architecture y base de datos propia.' },
    { name: 'SQL Server · RabbitMQ', detail: 'Persistencia y mensajería, listas para los próximos módulos.' },
    { name: 'LocalStack · Ollama', detail: 'Perfiles opcionales: AWS local (S3/SQS) e IA local.' },
  ];
}
