import { Injectable, inject } from '@angular/core';
import { APP_CONFIG } from '../config/app-config';

@Injectable({
  providedIn: 'root',
})
export class DevLoggerService {
  private readonly config = inject(APP_CONFIG, { optional: true });
  private readonly isDev = !this.config?.production;

  private readonly isBrowser = typeof window !== 'undefined' && typeof window.document !== 'undefined';

  resolveFeature(url: string): string {
    const lower = url.toLowerCase();
    if (lower.includes('/comments')) return 'COMMENTS';
    if (lower.includes('/posts')) return 'POSTS';
    if (lower.includes('/users') || lower.includes('/admin/users')) return 'USERS';
    if (lower.includes('/analytics')) return 'ANALYTICS';
    if (lower.includes('/auth') || lower.includes('supabase')) return 'AUTH';
    return 'API';
  }

  logRequest(feature: string, method: string, url: string): void {
    if (!this.isDev) return;

    if (this.isBrowser) {
      console.log(
        `%c[REQ]%c[${feature}]%c[${method}]%c ${url}`,
        'color: #38bdf8; font-weight: bold;',
        'color: #c084fc; font-weight: bold;',
        'color: #facc15; font-weight: bold;',
        'color: inherit;'
      );
    } else {
      console.log(`[REQ][${feature}][${method}] ${url}`);
    }
  }

  logResponse(feature: string, method: string, url: string, status: number, elapsedMs: number): void {
    if (!this.isDev) return;

    if (this.isBrowser) {
      console.log(
        `%c[RES]%c[${feature}]%c[${method}]%c ${url} - Status ${status} OK (${elapsedMs}ms)`,
        'color: #4ade80; font-weight: bold;',
        'color: #c084fc; font-weight: bold;',
        'color: #facc15; font-weight: bold;',
        'color: #a3e635;'
      );
    } else {
      console.log(`[RES][${feature}][${method}] ${url} - Status ${status} OK (${elapsedMs}ms)`);
    }
  }

  logError(feature: string, method: string, url: string, status: number, message: string, elapsedMs: number): void {
    if (!this.isDev) return;

    if (this.isBrowser) {
      console.error(
        `%c[ERR]%c[${feature}]%c[${method}]%c ${url} - Status ${status}: ${message} (${elapsedMs}ms)`,
        'color: #f87171; font-weight: bold;',
        'color: #c084fc; font-weight: bold;',
        'color: #facc15; font-weight: bold;',
        'color: #ef4444;'
      );
    } else {
      console.error(`[ERR][${feature}][${method}] ${url} - Status ${status}: ${message} (${elapsedMs}ms)`);
    }
  }
}
