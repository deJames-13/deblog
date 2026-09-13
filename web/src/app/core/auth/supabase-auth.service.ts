import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { createClient, Session, SupabaseClient, User } from '@supabase/supabase-js';
import { firstValueFrom } from 'rxjs';
import { APP_CONFIG, DEFAULT_APP_CONFIG } from '../config/app-config';
import { UserProfileDto } from '../models/api.dto';

const STORAGE_KEYS = {
  TOKEN: 'deblog_jwt_token',
  USER_ROLE: 'deblog_user_role',
  CACHED_PROFILE: 'deblog_cached_profile',
  IS_ADMIN: 'deblog_is_admin',
  LAST_AUTH_TIMESTAMP: 'deblog_last_auth_time',
};

function getInitialCachedProfile(): UserProfileDto | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = localStorage.getItem(STORAGE_KEYS.CACHED_PROFILE);
    return raw ? (JSON.parse(raw) as UserProfileDto) : null;
  } catch {
    return null;
  }
}

function getInitialIsAdmin(): boolean {
  if (typeof window === 'undefined') return false;
  try {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
    const isAdmin = localStorage.getItem(STORAGE_KEYS.IS_ADMIN);
    const role = localStorage.getItem(STORAGE_KEYS.USER_ROLE);
    return !!token && (isAdmin === 'true' || role === 'Admin');
  } catch {
    return false;
  }
}

@Injectable({
  providedIn: 'root',
})
export class SupabaseAuthService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG, { optional: true }) ?? DEFAULT_APP_CONFIG;
  private supabase: SupabaseClient | null = null;

  // Signal State - hydrated synchronously from cache to eliminate refresh wait/flicker
  readonly session = signal<Session | null>(null);
  readonly currentUser = signal<User | null>(null);
  readonly userProfile = signal<UserProfileDto | null>(getInitialCachedProfile());
  readonly isInitialized = signal<boolean>(false);
  readonly isCheckingAuth = signal<boolean>(
    typeof window !== 'undefined' && !!localStorage.getItem(STORAGE_KEYS.TOKEN)
  );
  readonly authError = signal<string | null>(null);

  // Derived / Managed Admin state - immediately true if cached valid admin session exists
  readonly isAdmin = signal<boolean>(getInitialIsAdmin());

  readonly token = computed<string | null>(() => {
    const sess = this.session();
    if (sess?.access_token) return sess.access_token;
    if (typeof window !== 'undefined') {
      return localStorage.getItem(STORAGE_KEYS.TOKEN);
    }
    return null;
  });

  readonly isAuthenticated = computed<boolean>(() => {
    return !!this.token();
  });

  constructor() {
    this.initSupabase();
  }

  private initSupabase(): void {
    if (typeof window === 'undefined') return;

    if (!this.config.supabaseUrl || !this.config.supabaseAnonKey) {
      this.isCheckingAuth.set(false);
      this.isInitialized.set(true);
      return;
    }

    try {
      this.supabase = createClient(this.config.supabaseUrl, this.config.supabaseAnonKey, {
        auth: {
          persistSession: true,
          autoRefreshToken: true,
          detectSessionInUrl: true,
        },
      });

      // Listen for auth state changes
      this.supabase.auth.onAuthStateChange(async (event, session) => {
        this.session.set(session);
        this.currentUser.set(session?.user ?? null);

        if (session?.access_token) {
          localStorage.setItem(STORAGE_KEYS.TOKEN, session.access_token);
          await this.syncCurrentUserProfile();
        } else if (event === 'SIGNED_OUT') {
          this.clearAuthStorage();
        }
      });

      // Restore and validate session in background
      this.supabase.auth
        .getSession()
        .then(async ({ data: { session } }) => {
          if (session) {
            this.session.set(session);
            this.currentUser.set(session.user);
            localStorage.setItem(STORAGE_KEYS.TOKEN, session.access_token);
            await this.syncCurrentUserProfile();
          } else {
            // No valid active session in Supabase; clear stale storage
            this.clearAuthStorage();
          }
          this.isCheckingAuth.set(false);
          this.isInitialized.set(true);
        })
        .catch((err) => {
          console.warn('[SupabaseAuth] Session restoration error:', err);
          this.isCheckingAuth.set(false);
          this.isInitialized.set(true);
        });
    } catch (err) {
      console.warn('[SupabaseAuth] Could not initialize client:', err);
      this.isCheckingAuth.set(false);
      this.isInitialized.set(true);
    }
  }

  /**
   * Signs in user with Supabase email & password
   */
  async signInWithPassword(email: string, password: string): Promise<{ success: boolean; error?: string }> {
    this.authError.set(null);

    if (!this.supabase) {
      const msg =
        'AUTHENTICATION_ERROR: SUPABASE_ANON_KEY is not configured in web/.env. Please paste your anon public key from Supabase Dashboard > Project Settings > API.';
      this.authError.set(msg);
      return { success: false, error: msg };
    }

    try {
      const { data, error } = await this.supabase.auth.signInWithPassword({
        email: email.trim(),
        password: password.trim(),
      });

      if (error) {
        this.authError.set(error.message);
        return { success: false, error: error.message };
      }

      if (data.session) {
        this.session.set(data.session);
        this.currentUser.set(data.user);
        if (typeof window !== 'undefined') {
          localStorage.setItem(STORAGE_KEYS.TOKEN, data.session.access_token);
        }

        await this.syncCurrentUserProfile();

        if (!this.isAdmin()) {
          await this.signOut();
          const deniedMsg = 'ACCESS_DENIED: Account lacks administrator privileges.';
          this.authError.set(deniedMsg);
          return { success: false, error: deniedMsg };
        }

        return { success: true };
      }

      const noSessionMsg = 'AUTHENTICATION_FAILED: No active session was created.';
      this.authError.set(noSessionMsg);
      return { success: false, error: noSessionMsg };
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Authentication failed';
      this.authError.set(msg);
      return { success: false, error: msg };
    }
  }

  /**
   * Syncs user profile with the .NET backend API /api/users/me
   */
  async syncCurrentUserProfile(): Promise<UserProfileDto | null> {
    try {
      const profile = await firstValueFrom(
        this.http.get<UserProfileDto>(`${this.config.apiUrl}/users/me`)
      );

      this.userProfile.set(profile);
      const isUserAdmin =
        profile.role === 'Admin' ||
        profile.email.toLowerCase() === this.config.authorEmail.toLowerCase();

      this.isAdmin.set(isUserAdmin);
      if (typeof window !== 'undefined') {
        localStorage.setItem(STORAGE_KEYS.USER_ROLE, profile.role);
        localStorage.setItem(STORAGE_KEYS.CACHED_PROFILE, JSON.stringify(profile));
        localStorage.setItem(STORAGE_KEYS.IS_ADMIN, String(isUserAdmin));
        localStorage.setItem(STORAGE_KEYS.LAST_AUTH_TIMESTAMP, String(Date.now()));
      }
      return profile;
    } catch (err: unknown) {
      // If server returned 401 or 403, revoke session immediately
      if (
        err &&
        typeof err === 'object' &&
        'status' in err &&
        (err.status === 401 || err.status === 403)
      ) {
        this.clearAuthStorage();
        return null;
      }

      // If server profile fetch fails (e.g. network offline), verify against currentUser email or cached profile
      const email =
        this.currentUser()?.email ?? this.userProfile()?.email ?? '';
      const isAuthor =
        !!email && email.toLowerCase() === this.config.authorEmail.toLowerCase();
      if (isAuthor) {
        this.isAdmin.set(true);
      }
      return this.userProfile();
    }
  }

  /**
   * Signs out current user
   */
  async signOut(): Promise<void> {
    if (this.supabase) {
      try {
        await this.supabase.auth.signOut();
      } catch (err) {
        console.warn('[SupabaseAuth] Error signing out:', err);
      }
    }
    this.clearAuthStorage();
  }

  private clearAuthStorage(): void {
    this.session.set(null);
    this.currentUser.set(null);
    this.userProfile.set(null);
    this.isAdmin.set(false);
    this.authError.set(null);

    if (typeof window !== 'undefined') {
      localStorage.removeItem(STORAGE_KEYS.TOKEN);
      localStorage.removeItem(STORAGE_KEYS.USER_ROLE);
      localStorage.removeItem(STORAGE_KEYS.CACHED_PROFILE);
      localStorage.removeItem(STORAGE_KEYS.IS_ADMIN);
      localStorage.removeItem(STORAGE_KEYS.LAST_AUTH_TIMESTAMP);
      localStorage.removeItem('bios_blog_is_admin');
    }
  }

  /**
   * Allows setting developer JWT token for manual backend testing
   */
  setDevToken(token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEYS.TOKEN, token);
      this.syncCurrentUserProfile();
    }
  }

  /**
   * Exposes the underlying initialized SupabaseClient instance
   */
  getSupabaseClient(): SupabaseClient | null {
    return this.supabase;
  }
}
