import { HttpErrorResponse } from '@angular/common/http';

/** Error HTTP normalizado que reciben los componentes y servicios de la aplicación. */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly url: string | null,
    /** Cuerpo de la respuesta (p. ej. ProblemDetails o el JSON de un health check con 503). */
    readonly body: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  static fromHttpError(error: HttpErrorResponse): ApiError {
    return new ApiError(describe(error), error.status, error.url, error.error);
  }
}

function describe(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'No fue posible contactar con el servidor. Verifica que el Gateway esté en ejecución.';
  }

  const problem = error.error as { title?: string; detail?: string } | null;
  const detail = problem?.detail ?? problem?.title;
  return detail ? `Error ${error.status}: ${detail}` : `Error ${error.status} al llamar a ${error.url ?? 'el servidor'}.`;
}
