import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { SupabaseAuthService } from './supabase-auth.service';

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authService = inject(SupabaseAuthService);
  const token = authService.token();

  // Attach token if request is directed to the backend API and token is present
  let authReq = req;
  if (token && (req.url.includes('/api') || req.url.startsWith('/api'))) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        console.warn('[AuthInterceptor] 401 Unauthorized encountered for:', req.url);
      } else if (error.status === 403) {
        console.warn('[AuthInterceptor] 403 Forbidden encountered for:', req.url);
      }
      return throwError(() => error);
    })
  );
};
