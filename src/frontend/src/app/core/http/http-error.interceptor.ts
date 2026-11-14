import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError } from './api-error';

/**
 * Convierte cualquier HttpErrorResponse en un ApiError con un mensaje comprensible.
 * Cada pantalla decide cómo mostrar el error; los no controlados llegan al GlobalErrorHandler.
 */
export const httpErrorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? ApiError.fromHttpError(error) : error)),
    ),
  );
