import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { DevLoggerService } from './dev-logger.service';

export const devLoggingInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const logger = inject(DevLoggerService);
  const startTime = Date.now();
  const feature = logger.resolveFeature(req.url);
  const method = req.method.toUpperCase();

  logger.logRequest(feature, method, req.urlWithParams);

  return next(req).pipe(
    tap((event: HttpEvent<unknown>) => {
      if (event instanceof HttpResponse) {
        const elapsed = Date.now() - startTime;
        logger.logResponse(feature, method, req.urlWithParams, event.status, elapsed);
      }
    }),
    catchError((error: HttpErrorResponse) => {
      const elapsed = Date.now() - startTime;
      const errorMsg = error.error?.message || error.statusText || error.message || 'Unknown Error';
      logger.logError(feature, method, req.urlWithParams, error.status, errorMsg, elapsed);
      return throwError(() => error);
    })
  );
};
