import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { devLoggingInterceptor } from './core/logging/dev-logging.interceptor';
import { provideAppConfig } from './core/config/app-config';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAppConfig(),
    provideHttpClient(withFetch(), withInterceptors([devLoggingInterceptor, authInterceptor])),
    provideRouter(routes),
  ],
};
