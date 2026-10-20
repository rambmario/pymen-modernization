import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProblemDetails } from './models';

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  private readonly snackBar = inject(MatSnackBar);

  showHttpError(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.snackBar.open('Error inesperado.', 'Cerrar', { duration: 5000 });
      return;
    }

    const message = this.messageFor(error);
    this.snackBar.open(message, 'Cerrar', { duration: 6000 });
  }

  messageFor(error: HttpErrorResponse): string {
    switch (error.status) {
      case 400:
      case 409:
      case 422:
        return this.fromProblemDetails(error) ?? `Error ${error.status}`;
      case 401:
        return 'Sesión expirada. Iniciá sesión de nuevo.';
      case 403:
        return 'No tenés permiso para esta acción.';
      case 404:
        return 'No se encontró el recurso solicitado.';
      case 429:
        return 'Demasiados intentos. Esperá un momento e intentá de nuevo.';
      default:
        return this.fromProblemDetails(error) ?? `Error del servidor (${error.status}).`;
    }
  }

  private fromProblemDetails(error: HttpErrorResponse): string | null {
    const body = error.error as ProblemDetails | string | null;
    if (typeof body === 'string' && body.trim()) {
      return body;
    }

    if (!body || typeof body !== 'object') {
      return null;
    }

    if (body.errors) {
      const messages = Object.values(body.errors).flat();
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    return body.detail || body.title || null;
  }
}
