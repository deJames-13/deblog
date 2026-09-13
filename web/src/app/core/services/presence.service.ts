import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { RealtimeChannel } from '@supabase/supabase-js';
import { SupabaseAuthService } from '../auth/supabase-auth.service';

export type AuthorPresenceStatus = 'online' | 'standby' | 'connecting' | 'error';

@Injectable({
  providedIn: 'root',
})
export class PresenceService {
  private readonly authService = inject(SupabaseAuthService);
  private presenceChannel: RealtimeChannel | null = null;

  // 5 Mandatory States coverage
  readonly presenceStatus = signal<AuthorPresenceStatus>('connecting');
  readonly isAuthorOnline = computed(() => this.presenceStatus() === 'online');
  readonly lastSeen = signal<Date | null>(null);

  constructor() {
    this.initPresence();

    // Re-evaluate tracking if auth status changes
    effect(() => {
      const isAdmin = this.authService.isAdmin();
      const isInit = this.authService.isInitialized();
      if (isInit && this.presenceChannel) {
        this.updateTracking(isAdmin);
      }
    });
  }

  private initPresence(): void {
    if (typeof window === 'undefined') {
      this.presenceStatus.set('standby');
      return;
    }

    const client = this.authService.getSupabaseClient();
    if (!client) {
      // Graceful fallback: If Supabase client is unconfigured, check if admin is logged in locally
      const localAdmin = this.authService.isAdmin();
      this.presenceStatus.set(localAdmin ? 'online' : 'standby');
      return;
    }

    try {
      this.presenceChannel = client.channel('author-presence', {
        config: {
          presence: {
            key: 'author',
          },
        },
      });

      this.presenceChannel
        .on('presence', { event: 'sync' }, () => {
          this.handlePresenceSync();
        })
        .on('presence', { event: 'join' }, ({ key, newPresences }) => {
          this.handlePresenceSync();
        })
        .on('presence', { event: 'leave' }, ({ key, leftPresences }) => {
          this.handlePresenceSync();
        })
        .subscribe(async (status) => {
          if (status === 'SUBSCRIBED') {
            await this.updateTracking(this.authService.isAdmin());
            this.handlePresenceSync();
          } else if (status === 'CHANNEL_ERROR' || status === 'TIMED_OUT') {
            console.warn('[PresenceService] Realtime channel status:', status);
            this.presenceStatus.set(this.authService.isAdmin() ? 'online' : 'standby');
          }
        });

      // Untrack cleanly on window unload
      window.addEventListener('beforeunload', () => {
        if (this.presenceChannel) {
          this.presenceChannel.untrack();
        }
      });
    } catch (err) {
      console.warn('[PresenceService] Failed to establish presence channel:', err);
      this.presenceStatus.set(this.authService.isAdmin() ? 'online' : 'standby');
    }
  }

  private async updateTracking(isAdmin: boolean): Promise<void> {
    if (!this.presenceChannel) return;

    try {
      if (isAdmin) {
        await this.presenceChannel.track({
          user: 'author',
          role: 'admin',
          online_at: new Date().toISOString(),
        });
        this.presenceStatus.set('online');
      } else {
        await this.presenceChannel.untrack();
      }
    } catch (err) {
      console.warn('[PresenceService] Error updating presence tracking:', err);
    }
  }

  private handlePresenceSync(): void {
    if (!this.presenceChannel) return;

    try {
      const state = this.presenceChannel.presenceState();
      // Check if any presence entries represent the admin/author
      let adminPresent = false;
      for (const key of Object.keys(state)) {
        const presences = state[key] as Array<{ role?: string; user?: string }>;
        if (
          key === 'author' ||
          presences.some((p) => p.role === 'admin' || p.user === 'author')
        ) {
          adminPresent = true;
          break;
        }
      }

      // If local session is admin, author is definitively online
      if (this.authService.isAdmin()) {
        adminPresent = true;
      }

      if (adminPresent) {
        this.presenceStatus.set('online');
      } else {
        this.presenceStatus.set('standby');
        this.lastSeen.set(new Date());
      }
    } catch (err) {
      console.warn('[PresenceService] Error resolving presence sync:', err);
      this.presenceStatus.set(this.authService.isAdmin() ? 'online' : 'standby');
    }
  }
}
