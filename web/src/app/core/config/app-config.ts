import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from '@angular/core';
import { generatedAppConfig } from './app-config.generated';

export interface AppConfig {
  production: boolean;
  apiUrl: string;
  supabaseUrl: string;
  supabaseAnonKey: string;
  authorEmail: string;
  useMockFallback: boolean;
}

export const DEFAULT_APP_CONFIG: AppConfig = {
  production: false,
  apiUrl: '/api',
  supabaseUrl: 'https://jtszesnaluflmaldqwme.supabase.co',
  supabaseAnonKey: '',
  authorEmail: 'de.james013@gmail.com',
  useMockFallback: true,
};

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG', {
  providedIn: 'root',
  factory: () => generatedAppConfig ?? DEFAULT_APP_CONFIG,
});

export function provideAppConfig(config?: Partial<AppConfig>): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: APP_CONFIG,
      useValue: { ...(generatedAppConfig ?? DEFAULT_APP_CONFIG), ...config },
    },
  ]);
}
