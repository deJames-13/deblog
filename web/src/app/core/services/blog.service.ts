import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';
import { APP_CONFIG, DEFAULT_APP_CONFIG } from '../config/app-config';
import {
  AdminTab,
  BlogComment,
  BlogPost,
  CommentStatus,
  MediaItem,
  PostStatus,
  SiteProfile,
  TelemetrySummary,
  ThemeMode,
  ToastMessage,
  TypographyFont,
  UserAccount,
  UserRole,
} from '../models/blog.model';
import {
  INITIAL_COMMENTS,
  INITIAL_MEDIA,
  INITIAL_POSTS,
  INITIAL_PROFILE,
  INITIAL_USERS,
} from '../data/mock-data';
import { filterProfanity } from '../data/profanity-list';
import {
  PostDetailDto,
  PostListItemDto,
  CommentResponseDto,
  CommentCreatedResponseDto,
  AdminCommentResponseDto,
  MediaStatusDto,
  UserProfileDto,
} from '../models/api.dto';
import { PostsApiService } from '../api/posts-api.service';
import { CommentsApiService } from '../api/comments-api.service';
import { UsersApiService } from '../api/users-api.service';
import { MediaApiService } from '../api/media-api.service';
import { AnalyticsApiService } from '../api/analytics-api.service';
import { SettingsApiService } from '../api/settings-api.service';
import { SupabaseAuthService } from '../auth/supabase-auth.service';

const LOCAL_STORAGE_KEYS = {
  THEME: 'bios_blog_theme',
  FONT: 'bios_blog_font',
  POSTS: 'bios_blog_posts',
  COMMENTS: 'bios_blog_comments',
  USERS: 'bios_blog_users',
  MEDIA: 'bios_blog_media',
  PROFILE: 'bios_blog_profile',
  LIKED: 'bios_blog_liked_ids',
};

function getInitialRoute(): 'landing' | 'post' | 'search' | 'admin' {
  if (typeof window === 'undefined') return 'landing';
  const path = window.location.pathname;
  if (path.startsWith('/admin')) return 'admin';
  if (path.startsWith('/post/')) return 'post';
  return 'landing';
}

function getInitialPostId(): string | null {
  if (typeof window === 'undefined') return null;
  const path = window.location.pathname;
  if (path.startsWith('/post/')) {
    const parts = path.split('/');
    return parts[2]?.split('?')[0]?.split('#')[0] || null;
  }
  return null;
}

function getInitialAdminTab(): AdminTab {
  if (typeof window === 'undefined') return 'dashboard';
  try {
    const params = new URLSearchParams(window.location.search);
    const tab = params.get('tab') as AdminTab;
    const validTabs: AdminTab[] = ['dashboard', 'posts', 'users', 'comments', 'media', 'settings'];
    return tab && validTabs.includes(tab) ? tab : 'dashboard';
  } catch {
    return 'dashboard';
  }
}

function getSaved<T>(key: string, fallback: T): T {
  if (typeof window === 'undefined' || !window.localStorage) return fallback;
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) : fallback;
  } catch {
    return fallback;
  }
}

function getSavedString(key: string, fallback: string): string {
  if (typeof window === 'undefined' || !window.localStorage) return fallback;
  try {
    const raw = localStorage.getItem(key);
    return raw || fallback;
  } catch {
    return fallback;
  }
}

function getSavedProfile(): SiteProfile {
  const p = getSaved<SiteProfile>(LOCAL_STORAGE_KEYS.PROFILE, INITIAL_PROFILE);
  if (!p.avatar_url || p.avatar_url.includes('AVATAR_DARK.png') || p.avatar_url.includes('derickespinosa.site')) {
    p.avatar_url = 'public/assets/images/me.png';
  }
  return p;
}

function mapPostDtoToBlogPost(dto: PostListItemDto | PostDetailDto, existingPost?: BlogPost): BlogPost {
  let statusStr: PostStatus = 'draft';
  if (typeof dto.status === 'string') {
    statusStr = dto.status.toLowerCase() as PostStatus;
  } else if (dto.status === 1) {
    statusStr = 'published';
  } else if (dto.status === 2) {
    statusStr = 'hidden';
  } else if (dto.status === 3) {
    statusStr = 'archived';
  }

  const content = 'content' in dto && dto.content ? dto.content : (existingPost?.content || '');
  const summary = (dto.summary || '').trim();
  const wordCount = (content || summary).split(/\s+/).filter(Boolean).length;
  const readingTime = Math.max(1, Math.ceil(wordCount / 200));

  return {
    id: dto.id,
    slug: dto.slug,
    title: dto.title,
    subtitle: summary,
    excerpt: summary || (content ? content.replace(/[#*`_~>[\]]/g, '').slice(0, 160).trim() + '...' : 'No summary provided.'),
    content: content,
    cover_image: dto.coverImageUrl || undefined,
    category: dto.category || 'Engineering',
    tags: dto.tags && dto.tags.length > 0 ? dto.tags : ['tech'],
    status: statusStr,
    likes_count: dto.analytics?.likes ?? 0,
    views_count: dto.analytics?.views ?? 0,
    reading_time_minutes: readingTime,
    created_at: dto.createdAt,
    updated_at: 'updatedAt' in dto ? dto.updatedAt : (existingPost?.updated_at || dto.createdAt),
    published_at: dto.publishedAt || undefined,
    is_featured: dto.isFeatured ?? false,
  };
}

function mapCommentDtoToBlogComment(dto: CommentResponseDto, postTitle = 'Article Discussion'): BlogComment {
  let statusStr: CommentStatus = 'pending';
  if (typeof dto.status === 'string') {
    statusStr = dto.status.toLowerCase() as CommentStatus;
  } else if (dto.status === 1) {
    statusStr = 'approved';
  } else if (dto.status === 2) {
    statusStr = 'rejected';
  } else if (dto.status === 3) {
    statusStr = 'spam';
  }

  return {
    id: dto.id,
    post_id: dto.postId,
    post_title: postTitle,
    author_name: dto.author?.displayName || dto.author?.username || 'Guest Reader',
    author_email: 'verified-guest@deblog.local',
    content: dto.content,
    status: statusStr,
    likes_count: 0,
    created_at: dto.createdAt,
  };
}

function mapAdminCommentDtoToBlogComment(dto: AdminCommentResponseDto): BlogComment {
  let statusStr: CommentStatus = 'pending';
  if (dto.status === 1 || dto.status === 'Approved') {
    statusStr = 'approved';
  } else if (dto.status === 2 || dto.status === 'Rejected') {
    statusStr = 'rejected';
  } else if (dto.status === 3 || dto.status === 'Spam') {
    statusStr = 'spam';
  }

  return {
    id: dto.id,
    post_id: dto.postId,
    post_title: dto.postTitle || 'Article Entry',
    author_name: dto.author?.displayName || dto.author?.username || 'Guest Reader',
    author_email: 'verified-guest@deblog.local',
    content: dto.content,
    status: statusStr,
    likes_count: 0,
    created_at: dto.createdAt,
  };
}

function mapUserDtoToUserAccount(dto: UserProfileDto): UserAccount {
  const roleLower = (dto.role || 'user').toLowerCase();
  const role: UserRole = roleLower === 'admin' ? 'admin' : 'commenter';
  const isBanned = dto.status === 2 || dto.status === 'Banned' || dto.status === 'Suspended';

  return {
    id: dto.id,
    name: dto.displayName || dto.username || 'Community Member',
    email: dto.email,
    role,
    status: isBanned ? 'banned' : 'active',
    comments_count: 0,
    created_at: dto.createdAt,
    last_active_at: dto.createdAt,
  };
}

function havePostsChanged(current: BlogPost[], next: BlogPost[]): boolean {
  if (current.length !== next.length) return true;
  for (let i = 0; i < current.length; i++) {
    const a = current[i];
    const b = next[i];
    if (
      a.id !== b.id ||
      a.status !== b.status ||
      a.title !== b.title ||
      a.excerpt !== b.excerpt ||
      a.content !== b.content ||
      a.cover_image !== b.cover_image ||
      a.views_count !== b.views_count ||
      a.likes_count !== b.likes_count ||
      a.updated_at !== b.updated_at ||
      a.published_at !== b.published_at ||
      a.is_featured !== b.is_featured
    ) {
      return true;
    }
  }
  return false;
}

function haveCommentsChanged(current: BlogComment[], next: BlogComment[]): boolean {
  if (current.length !== next.length) return true;
  for (let i = 0; i < current.length; i++) {
    const a = current[i];
    const b = next[i];
    if (
      a.id !== b.id ||
      a.status !== b.status ||
      a.content !== b.content ||
      a.likes_count !== b.likes_count ||
      a.author_name !== b.author_name
    ) {
      return true;
    }
  }
  return false;
}

function haveUsersChanged(current: UserAccount[], next: UserAccount[]): boolean {
  if (current.length !== next.length) return true;
  for (let i = 0; i < current.length; i++) {
    const a = current[i];
    const b = next[i];
    if (
      a.id !== b.id ||
      a.role !== b.role ||
      a.status !== b.status ||
      a.name !== b.name ||
      a.email !== b.email
    ) {
      return true;
    }
  }
  return false;
}

@Injectable({
  providedIn: 'root',
})
export class BlogService {
  private readonly router = inject(Router);
  private readonly config = inject(APP_CONFIG, { optional: true }) ?? DEFAULT_APP_CONFIG;
  private readonly postsApi = inject(PostsApiService);
  private readonly commentsApi = inject(CommentsApiService);
  private readonly usersApi = inject(UsersApiService);
  private readonly mediaApi = inject(MediaApiService);
  private readonly analyticsApi = inject(AnalyticsApiService);
  private readonly settingsApi = inject(SettingsApiService);
  readonly authService = inject(SupabaseAuthService);

  // Telemetry & Media Signals
  readonly mediaStatus = signal<MediaStatusDto | null>(null);
  readonly cloudinaryConfigured = signal<boolean>(false);
  readonly isMediaLoading = signal<boolean>(false);
  readonly telemetrySummary = signal<TelemetrySummary | null>(null);
  readonly isTelemetryLoading = signal<boolean>(false);

  // Appearance Signals
  readonly theme = signal<ThemeMode>(
    (getSavedString(LOCAL_STORAGE_KEYS.THEME, 'twilight') as ThemeMode) || 'twilight'
  );
  readonly font = signal<TypographyFont>(
    (getSavedString(LOCAL_STORAGE_KEYS.FONT, 'roboto') as TypographyFont) || 'roboto'
  );
  readonly zenMode = signal<boolean>(false);
  readonly sidebarOpen = signal<boolean>(true);

  // Navigation Signals
  readonly currentRoute = signal<'landing' | 'post' | 'search' | 'admin'>(getInitialRoute());
  readonly selectedPostId = signal<string | null>(getInitialPostId());
  readonly adminTab = signal<AdminTab>(getInitialAdminTab());

  // Search & Filter Signals
  readonly searchQuery = signal<string>('');
  readonly selectedCategory = signal<string | null>(null);
  readonly selectedMonth = signal<string | null>(null);
  readonly currentPage = signal<number>(1);

  // Loading & Offline Fallback States (5 Mandatory States Support)
  readonly isLoading = signal<boolean>(false);
  readonly isDetailLoading = signal<boolean>(false);
  readonly isSyncing = signal<boolean>(false);
  readonly isOfflineFallback = signal<boolean>(false);
  readonly apiError = signal<string | null>(null);
  readonly lastSyncedAt = signal<Date | null>(null);

  // Auth Signal - strictly synchronized with SupabaseAuthService
  readonly isAdmin = signal<boolean>(this.authService.isAdmin());
  readonly isCheckingAuth = this.authService.isCheckingAuth;

  // Modal Signals
  readonly commentModalOpen = signal<boolean>(false);
  readonly commentTargetPostId = signal<string | null>(null);
  readonly helpModalOpen = signal<boolean>(false);

  // Liked Posts Signal
  readonly likedPosts = signal<string[]>(getSaved<string[]>(LOCAL_STORAGE_KEYS.LIKED, []));

  // Core Data Signals
  readonly posts = signal<BlogPost[]>(getSaved<BlogPost[]>(LOCAL_STORAGE_KEYS.POSTS, INITIAL_POSTS));
  readonly comments = signal<BlogComment[]>(
    getSaved<BlogComment[]>(LOCAL_STORAGE_KEYS.COMMENTS, INITIAL_COMMENTS)
  );
  readonly users = signal<UserAccount[]>(getSaved<UserAccount[]>(LOCAL_STORAGE_KEYS.USERS, INITIAL_USERS));
  readonly media = signal<MediaItem[]>(getSaved<MediaItem[]>(LOCAL_STORAGE_KEYS.MEDIA, INITIAL_MEDIA));
  readonly profile = signal<SiteProfile>(getSavedProfile());

  // Toasts Signal
  readonly toasts = signal<ToastMessage[]>([]);

  // Derived / Computed Signals
  readonly activePost = computed(() => {
    const id = this.selectedPostId();
    const list = this.posts();
    return list.find((p) => p.id === id || p.slug === id) || list[0] || null;
  });

  readonly publishedPosts = computed(() => {
    return this.posts().filter((p) => p.status === 'published');
  });

  readonly pendingCommentsCount = computed(() => {
    return this.comments().filter((c) => c.status === 'pending').length;
  });

  readonly draftPostsCount = computed(() => {
    return this.posts().filter((p) => p.status === 'draft').length;
  });

  constructor() {
    // Keep auth state synchronized with SupabaseAuthService
    effect(() => {
      const authIsAdmin = this.authService.isAdmin();
      this.isAdmin.set(authIsAdmin);
    });

    // Clear legacy mock auth flag if not authenticated
    if (typeof localStorage !== 'undefined') {
      if (!this.authService.isAuthenticated()) {
        localStorage.removeItem('bios_blog_is_admin');
      }
    }

    // Synchronize HTML attributes & localStorage with theme & font
    effect(() => {
      const currentTheme = this.theme();
      if (typeof document !== 'undefined') {
        document.documentElement.setAttribute('data-theme', currentTheme);
      }
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.THEME, currentTheme);
      }
    });

    effect(() => {
      const currentFont = this.font();
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.FONT, currentFont);
      }
    });

    // Auto-save changes to localStorage as fallback cache
    effect(() => {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.POSTS, JSON.stringify(this.posts()));
      }
    });

    effect(() => {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.COMMENTS, JSON.stringify(this.comments()));
      }
    });

    effect(() => {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.USERS, JSON.stringify(this.users()));
      }
    });

    effect(() => {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.MEDIA, JSON.stringify(this.media()));
      }
    });

    effect(() => {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.PROFILE, JSON.stringify(this.profile()));
      }
    });

    effect(() => {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.LIKED, JSON.stringify(this.likedPosts()));
      }
    });

    // Handle router navigation synchronization
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        const url = e.urlAfterRedirects || e.url;
        if (url.startsWith('/post/')) {
          const parts = url.split('/');
          const id = parts[2]?.split('?')[0]?.split('#')[0];
          if (id) {
            this.selectedPostId.set(id);
            this.fetchPostDetail(id);
          }
          this.currentRoute.set('post');
        } else if (url.startsWith('/admin-auth')) {
          this.currentRoute.set('admin');
          this.router.navigate(['/admin'], { queryParamsHandling: 'preserve', replaceUrl: true });
        } else if (url.startsWith('/admin')) {
          this.currentRoute.set('admin');
          const queryStr = url.includes('?') ? url.split('?')[1] : '';
          const searchParams = new URLSearchParams(queryStr);
          const tab = searchParams.get('tab') as AdminTab | null;
          const validTabs: AdminTab[] = ['dashboard', 'posts', 'users', 'comments', 'media', 'settings'];
          if (tab && validTabs.includes(tab)) {
            this.adminTab.set(tab);
          }
        } else if (url === '/' || url === '') {
          this.currentRoute.set('landing');
        }
      });

    // Initial Live Data Load & Start Background Sync
    this.loadInitialData();
    this.startBackgroundSync();
  }

  private syncIntervalTimer: ReturnType<typeof setInterval> | null = null;
  private visibilityListener: (() => void) | null = null;

  /**
   * Starts periodic background synchronization with backend database.
   * Also listens for document visibility change to sync immediately on window focus.
   * Default interval is 10 minutes (600,000 ms).
   */
  startBackgroundSync(intervalMs = 600000): void {
    this.stopBackgroundSync();

    if (typeof window !== 'undefined') {
      this.syncIntervalTimer = setInterval(() => {
        this.syncFromBackend({ silent: true });
      }, intervalMs);

      this.visibilityListener = () => {
        if (typeof document !== 'undefined' && document.visibilityState === 'visible') {
          this.syncFromBackend({ silent: true });
        }
      };
      document.addEventListener('visibilitychange', this.visibilityListener);
    }
  }

  /**
   * Stops background synchronization.
   */
  stopBackgroundSync(): void {
    if (this.syncIntervalTimer) {
      clearInterval(this.syncIntervalTimer);
      this.syncIntervalTimer = null;
    }
    if (typeof document !== 'undefined' && this.visibilityListener) {
      document.removeEventListener('visibilitychange', this.visibilityListener);
      this.visibilityListener = null;
    }
  }

  /**
   * Synchronizes data with the live backend database, checking differences before triggering rerenders.
   * If silent is true (default during background sync), isLoading is NOT set, preventing skeleton flashes.
   */
  async syncFromBackend(options?: { silent?: boolean }): Promise<boolean> {
    const silent = options?.silent ?? false;
    this.isSyncing.set(true);
    if (!silent && this.posts().length === 0) {
      this.isLoading.set(true);
    }
    let hasChanges = false;
    try {
      const result = await firstValueFrom(
        this.postsApi.getPosts({ page: 1, pageSize: 50, publishedOnly: !this.isAdmin() })
      );

      if (result && result.items) {
        const existingMap = new Map(this.posts().map((p) => [p.id, p]));
        const mapped = result.items.map((dto) => mapPostDtoToBlogPost(dto, existingMap.get(dto.id)));
        if (havePostsChanged(this.posts(), mapped)) {
          this.posts.set(mapped);
          hasChanges = true;
        }
        this.isOfflineFallback.set(false);
        this.lastSyncedAt.set(new Date());
      }

      // If admin is active, sync admin data as well silently
      if (this.isAdmin()) {
        await Promise.allSettled([
          this.loadAdminComments(silent),
          this.loadAdminUsers(silent),
        ]);
      }
      return hasChanges;
    } catch (err: unknown) {
      console.warn('[BlogService] Backend sync failed:', err);
      if (this.config.useMockFallback && this.posts().length === 0) {
        this.isOfflineFallback.set(true);
        this.posts.set(INITIAL_POSTS);
      }
      return false;
    } finally {
      this.isSyncing.set(false);
      if (!silent) {
        this.isLoading.set(false);
      }
    }
  }

  /**
   * Initial data load: attempts live backend fetch with graceful mock fallback
   */
  async loadInitialData(): Promise<void> {
    if (typeof window === 'undefined') return;

    this.isLoading.set(true);
    this.apiError.set(null);
    await this.syncFromBackend({ silent: false });
  }

  /**
   * Loads posts for admin management (includes drafts, hidden, archived)
   */
  async loadAdminPosts(silent = false): Promise<void> {
    if (!silent) {
      this.isLoading.set(true);
    }
    this.apiError.set(null);
    try {
      const result = await firstValueFrom(
        this.postsApi.getPosts({ page: 1, pageSize: 50, publishedOnly: false })
      );
      if (result && result.items) {
        const existingMap = new Map(this.posts().map((p) => [p.id, p]));
        const mapped = result.items.map((dto) => mapPostDtoToBlogPost(dto, existingMap.get(dto.id)));
        if (havePostsChanged(this.posts(), mapped)) {
          this.posts.set(mapped);
        }
        this.isOfflineFallback.set(false);
      }
    } catch (err: unknown) {
      console.warn('[BlogService] Backend admin posts fetch failed, evaluating fallback:', err);
      if (this.config.useMockFallback) {
        this.isOfflineFallback.set(true);
      } else {
        this.apiError.set('Failed to retrieve admin posts from backend.');
      }
    } finally {
      if (!silent) {
        this.isLoading.set(false);
      }
    }
  }

  /**
   * Loads all comments for admin moderation from backend
   */
  async loadAdminComments(silent = false): Promise<void> {
    if (!silent) {
      this.isLoading.set(true);
    }
    this.apiError.set(null);
    try {
      const result = await firstValueFrom(
        this.commentsApi.getAdminComments({ page: 1, pageSize: 50 })
      );
      if (result && result.items) {
        const mapped = result.items.map(mapAdminCommentDtoToBlogComment);
        if (haveCommentsChanged(this.comments(), mapped)) {
          this.comments.set(mapped);
        }
        this.isOfflineFallback.set(false);
      }
    } catch (err: unknown) {
      console.warn('[BlogService] Backend admin comments fetch failed, evaluating fallback:', err);
      if (this.config.useMockFallback) {
        this.isOfflineFallback.set(true);
      } else {
        this.apiError.set('Failed to retrieve admin comments from backend.');
      }
    } finally {
      if (!silent) {
        this.isLoading.set(false);
      }
    }
  }

  /**
   * Loads all registered users for admin management from backend
   */
  async loadAdminUsers(silent = false): Promise<void> {
    if (!silent) {
      this.isLoading.set(true);
    }
    this.apiError.set(null);
    try {
      const result = await firstValueFrom(
        this.usersApi.getAdminUsers({ page: 1, pageSize: 50 })
      );
      if (result && result.items) {
        const mapped = result.items.map(mapUserDtoToUserAccount);
        if (haveUsersChanged(this.users(), mapped)) {
          this.users.set(mapped);
        }
        this.isOfflineFallback.set(false);
      }
    } catch (err: unknown) {
      console.warn('[BlogService] Backend admin users fetch failed, evaluating fallback:', err);
      if (this.config.useMockFallback) {
        this.isOfflineFallback.set(true);
      } else {
        this.apiError.set('Failed to retrieve admin users from backend.');
      }
    } finally {
      if (!silent) {
        this.isLoading.set(false);
      }
    }
  }

  /**
   * Fetches full post detail by ID or slug from the live backend
   */
  async fetchPostDetail(idOrSlug: string): Promise<BlogPost | null> {
    const existing = this.posts().find((p) => p.id === idOrSlug || p.slug === idOrSlug);
    const needsLoading = !existing || !existing.content;
    if (needsLoading) {
      this.isDetailLoading.set(true);
    }
    try {
      const detailDto = await firstValueFrom(this.postsApi.getPostByIdOrSlug(idOrSlug));
      if (detailDto) {
        const blogPost = mapPostDtoToBlogPost(detailDto);
        this.posts.update((prev) => {
          const index = prev.findIndex((p) => p.id === blogPost.id || p.slug === blogPost.slug);
          if (index >= 0) {
            const copy = [...prev];
            copy[index] = blogPost;
            return copy;
          }
          return [blogPost, ...prev];
        });

        // Also fetch approved comments for this post
        this.fetchPostComments(detailDto.id);
        return blogPost;
      }
    } catch (err) {
      console.warn(`[BlogService] Could not fetch detail for ${idOrSlug} from backend:`, err);
    } finally {
      if (needsLoading) {
        this.isDetailLoading.set(false);
      }
    }
    return this.posts().find((p) => p.id === idOrSlug || p.slug === idOrSlug) || null;
  }

  /**
   * Fetches comments for a specific post
   */
  async fetchPostComments(postId: string): Promise<void> {
    try {
      const commentsDtos = await firstValueFrom(this.commentsApi.getPostComments(postId));
      if (commentsDtos && commentsDtos.length > 0) {
        const activeTitle = this.activePost()?.title || 'Article Entry';
        const mappedComments = commentsDtos.map((c) => mapCommentDtoToBlogComment(c, activeTitle));
        this.comments.update((prev) => {
          const otherComments = prev.filter((c) => c.post_id !== postId);
          return [...mappedComments, ...otherComments];
        });
      }
    } catch (err) {
      console.warn(`[BlogService] Could not fetch comments for post ${postId}:`, err);
    }
  }

  // Appearance Methods
  setTheme(newTheme: ThemeMode): void {
    this.theme.set(newTheme);
    this.addToast(`Theme set to: ${newTheme.toUpperCase()}`, 'info');
  }

  setFont(newFont: TypographyFont): void {
    this.font.set(newFont);
    this.addToast(`Typography font: ${newFont.toUpperCase()}`, 'info');
  }

  toggleZenMode(): void {
    const next = !this.zenMode();
    this.zenMode.set(next);
    this.addToast(next ? 'Zen Mode Enabled [Esc to Exit]' : 'Zen Mode Disabled', 'info');
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  // Navigation Methods
  navigateTo(route: 'landing' | 'post' | 'search' | 'admin', postId?: string): void {
    if (route === 'post' && postId) {
      this.selectedPostId.set(postId);
      const targetPost = this.posts().find((p) => p.id === postId || p.slug === postId);

      // Increment view count locally
      this.posts.update((prev) =>
        prev.map((p) => (p.id === postId || p.slug === postId ? { ...p, views_count: p.views_count + 1 } : p))
      );

      // Fire analytics view tracking to backend asynchronously
      if (targetPost) {
        this.postsApi.trackView(targetPost.slug || targetPost.id).subscribe({
          next: () => {},
          error: () => {},
        });
      }

      // Fetch complete markdown content if not yet loaded in cache
      if (!targetPost?.content) {
        this.fetchPostDetail(targetPost?.slug || postId);
      }

      this.currentRoute.set('post');
      this.router.navigate(['/post', targetPost?.slug || postId]);
    } else if (route === 'admin') {
      this.currentRoute.set('admin');
      this.router.navigate(['/admin'], {
        queryParams: { tab: this.adminTab() },
      });
    } else {
      this.currentRoute.set('landing');
      this.router.navigate(['/']);
    }

    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  setAdminTab(tab: AdminTab): void {
    this.adminTab.set(tab);
    if (this.isAdmin()) {
      if (tab === 'posts') {
        this.loadAdminPosts();
      } else if (tab === 'comments') {
        this.loadAdminComments();
      } else if (tab === 'users') {
        this.loadAdminUsers();
      }
    }
    if (typeof window !== 'undefined' && this.currentRoute() === 'admin') {
      this.router.navigate(['/admin'], {
        queryParams: { tab },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    }
  }

  // Search & Filter Methods
  setSearchQuery(q: string): void {
    this.searchQuery.set(q);
  }

  setSelectedCategory(cat: string | null): void {
    this.selectedCategory.set(cat);
  }

  setSelectedMonth(m: string | null): void {
    this.selectedMonth.set(m);
  }

  setCurrentPage(p: number): void {
    this.currentPage.set(p);
  }

  // Like Methods
  hasLikedPost(postId: string): boolean {
    return this.likedPosts().includes(postId);
  }

  likePost(postId: string): void {
    const post = this.posts().find((p) => p.id === postId || p.slug === postId);
    if (!post) return;

    if (this.hasLikedPost(post.id)) {
      this.likedPosts.update((prev) => prev.filter((id) => id !== post.id));
      this.posts.update((prev) =>
        prev.map((p) => (p.id === post.id ? { ...p, likes_count: Math.max(0, p.likes_count - 1) } : p))
      );
      this.addToast('Unliked post', 'info');
    } else {
      this.likedPosts.update((prev) => [...prev, post.id]);
      this.posts.update((prev) =>
        prev.map((p) => (p.id === post.id ? { ...p, likes_count: p.likes_count + 1 } : p))
      );
      this.addToast('Article Liked! Thank you.', 'success');

      // Send like analytics to live backend
      this.postsApi.trackLike(post.slug || post.id).subscribe({
        next: () => {},
        error: () => {},
      });
    }
  }

  // Comment Methods
  openCommentModal(postId?: string): void {
    const target = postId || this.selectedPostId() || this.posts()[0]?.id || null;
    this.commentTargetPostId.set(target);
    this.commentModalOpen.set(true);
  }

  closeCommentModal(): void {
    this.commentModalOpen.set(false);
    this.commentTargetPostId.set(null);
  }

  openHelpModal(): void {
    this.helpModalOpen.set(true);
  }

  closeHelpModal(): void {
    this.helpModalOpen.set(false);
  }

  submitComment(params: {
    postId: string;
    authorName?: string;
    authorEmail: string;
    content: string;
  }): { success: boolean; message: string; flaggedWords?: string[] } {
    const { postId, authorName, authorEmail, content } = params;

    if (!authorEmail || !authorEmail.includes('@') || !authorEmail.includes('.')) {
      return {
        success: false,
        message: 'Invalid email address. A valid email is required to verify humanity.',
      };
    }

    if (!content || content.trim().length < 3) {
      return {
        success: false,
        message: 'Comment content cannot be empty or too short.',
      };
    }

    const filterResult = filterProfanity(content);
    if (filterResult.hasBadWords) {
      return {
        success: false,
        message: `Your comment contains restricted language: "${filterResult.detectedWords.join(', ')}". Please keep discussions professional.`,
        flaggedWords: filterResult.detectedWords,
      };
    }

    const post = this.posts().find((p) => p.id === postId || p.slug === postId);
    const resolvedPostId = post?.id || postId;

    const newComment: BlogComment = {
      id: `comm-${Date.now()}`,
      post_id: resolvedPostId,
      post_title: post?.title || 'Personal Entry',
      author_name: authorName?.trim() || 'Anonymous Guest',
      author_email: authorEmail.trim(),
      content: content.trim(),
      status: 'pending',
      likes_count: 0,
      created_at: new Date().toISOString(),
    };

    // Optimistically add to local comments
    this.comments.update((prev) => [newComment, ...prev]);

    // Send comment to backend
    this.commentsApi
      .createComment(resolvedPostId, {
        content: content.trim(),
        email: authorEmail.trim(),
        displayName: authorName?.trim(),
      })
      .subscribe({
        next: (created: CommentCreatedResponseDto) => {
          this.commentsApi.saveCommentToken(created.id, created.managementToken);
          this.comments.update((prev) =>
            prev.map((c) => (c.id === newComment.id ? { ...c, id: created.id } : c))
          );
        },
        error: (err: unknown) => {
          console.warn('[BlogService] Live comment creation failed, kept locally:', err);
        },
      });

    // Ensure commenter is recorded in users table
    this.users.update((prev) => {
      const existing = prev.find((u) => u.email.toLowerCase() === authorEmail.toLowerCase());
      if (existing) {
        return prev.map((u) =>
          u.id === existing.id
            ? { ...u, comments_count: u.comments_count + 1, last_active_at: new Date().toISOString() }
            : u
        );
      }
      return [
        ...prev,
        {
          id: `user-${Date.now()}`,
          name: authorName?.trim() || 'Guest Reader',
          email: authorEmail.trim(),
          role: 'commenter',
          status: 'active',
          comments_count: 1,
          created_at: new Date().toISOString(),
          last_active_at: new Date().toISOString(),
        },
      ];
    });

    this.closeCommentModal();
    this.addToast('Comment submitted! It is now pending moderation approval.', 'success');
    return { success: true, message: 'Comment submitted for approval' };
  }

  updateCommentStatus(id: string, status: CommentStatus): void {
    this.comments.update((prev) => prev.map((c) => (c.id === id ? { ...c, status } : c)));
    this.addToast(`Comment marked as ${status.toUpperCase()}`, 'info');

    // Sync status change to backend if Admin
    if (this.isAdmin()) {
      const statusMap: Record<CommentStatus, string> = {
        pending: 'Pending',
        approved: 'Approved',
        rejected: 'Rejected',
        spam: 'Spam',
      };
      this.commentsApi.updateCommentStatus(id, statusMap[status]).subscribe({
        next: () => {},
        error: (err: unknown) => console.warn('[BlogService] Backend comment status update failed:', err),
      });
    }
  }

  deleteComment(id: string): void {
    this.comments.update((prev) => prev.filter((c) => c.id !== id));
    this.addToast('Comment deleted', 'warning');

    this.commentsApi.deleteComment(id).subscribe({
      next: () => {},
      error: (err: unknown) => console.warn('[BlogService] Backend comment delete failed:', err),
    });
  }

  batchUpdateComments(ids: string[], status: CommentStatus): void {
    this.comments.update((prev) => prev.map((c) => (ids.includes(c.id) ? { ...c, status } : c)));
    this.addToast(`${ids.length} comments updated to ${status.toUpperCase()}`, 'success');

    if (this.isAdmin()) {
      ids.forEach((id) => this.updateCommentStatus(id, status));
    }
  }

  batchDeleteComments(ids: string[]): void {
    this.comments.update((prev) => prev.filter((c) => !ids.includes(c.id)));
    this.addToast(`${ids.length} comments permanently removed`, 'warning');

    ids.forEach((id) => this.deleteComment(id));
  }

  // Post CRUD Methods
  createPost(data: Partial<BlogPost>): BlogPost {
    const slug = (data.slug || data.title || 'untitled-post')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/(^-|-$)+/g, '');

    const rawSummary = (data.subtitle ?? data.excerpt ?? '').trim();
    const cleanContentPreview = data.content
      ? data.content.replace(/[#*`_~>[\]]/g, '').slice(0, 160).trim() + '...'
      : 'No summary provided.';
    const finalSummary = rawSummary || cleanContentPreview;

    const newPost: BlogPost = {
      id: `post-${Date.now()}`,
      slug,
      title: data.title || 'Untitled Post',
      subtitle: rawSummary,
      excerpt: finalSummary,
      content: data.content || '',
      cover_image:
        data.cover_image ||
        'https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=1000&q=80',
      category: data.category || 'Engineering',
      tags: data.tags || ['Engineering'],
      status: data.status || 'draft',
      likes_count: 0,
      views_count: 0,
      reading_time_minutes: Math.max(
        1,
        Math.ceil((data.content || '').split(/\s+/).length / 200)
      ),
      created_at: new Date().toISOString(),
      updated_at: new Date().toISOString(),
      published_at: data.status === 'published' ? new Date().toISOString() : undefined,
      is_featured: !!data.is_featured,
    };

    this.posts.update((prev) => [newPost, ...prev]);

    // Send to backend API
    this.postsApi
      .createPost({
        title: newPost.title,
        slug: newPost.slug,
        summary: rawSummary || undefined,
        content: newPost.content,
        status: newPost.status === 'published' ? 'Published' : 'Draft',
        coverImageUrl: newPost.cover_image,
        category: newPost.category,
        tags: newPost.tags,
        isFeatured: newPost.is_featured,
      })
      .subscribe({
        next: (created: PostDetailDto) => {
          this.posts.update((prev) =>
            prev.map((p) => (p.id === newPost.id ? mapPostDtoToBlogPost(created) : p))
          );
        },
        error: (err: unknown) => {
          console.warn('[BlogService] Post creation failed on backend, kept locally:', err);
        },
      });

    this.addToast(`Post "${newPost.title}" created (${newPost.status})`, 'success');
    return newPost;
  }

  updatePost(postId: string, data: Partial<BlogPost>): void {
    const rawSummary = (data.subtitle !== undefined ? data.subtitle : data.excerpt)?.trim();

    this.posts.update((prev) =>
      prev.map((p) => {
        if (p.id === postId) {
          const summaryValue = rawSummary !== undefined ? rawSummary : p.subtitle;
          const cleanPreview = data.content
            ? data.content.replace(/[#*`_~>[\]]/g, '').slice(0, 160).trim() + '...'
            : p.excerpt;
          const updated = {
            ...p,
            ...data,
            subtitle: summaryValue,
            excerpt: summaryValue || cleanPreview,
            updated_at: new Date().toISOString(),
            reading_time_minutes: data.content
              ? Math.max(1, Math.ceil(data.content.split(/\s+/).length / 200))
              : p.reading_time_minutes,
          };
          if (data.status === 'published' && !p.published_at) {
            updated.published_at = new Date().toISOString();
          }
          return updated;
        }
        return p;
      })
    );

    // Dispatch update to backend if post ID is valid Guid
    if (postId.length > 20 && postId.includes('-') && !postId.startsWith('post-')) {
      this.postsApi
        .updatePost(postId, {
          title: data.title,
          slug: data.slug,
          summary: rawSummary,
          content: data.content,
          status: data.status === 'published' ? 'Published' : data.status === 'draft' ? 'Draft' : undefined,
          coverImageUrl: data.cover_image,
          category: data.category,
          tags: data.tags,
          isFeatured: data.is_featured,
        })
        .subscribe({
          next: () => {},
          error: (err: unknown) => console.warn('[BlogService] Backend update failed:', err),
        });
    }

    this.addToast('Post updated successfully', 'success');
  }

  updatePostStatus(postId: string, status: PostStatus): void {
    this.posts.update((prev) =>
      prev.map((p) =>
        p.id === postId ? { ...p, status, updated_at: new Date().toISOString() } : p
      )
    );

    if (postId.length > 20 && postId.includes('-') && !postId.startsWith('post-')) {
      const statusMap: Record<PostStatus, string> = {
        published: 'Published',
        draft: 'Draft',
        hidden: 'Hidden',
        archived: 'Archived',
      };
      this.postsApi.updatePostStatus(postId, statusMap[status]).subscribe({
        next: () => {},
        error: (err: unknown) => console.warn('[BlogService] Backend status update failed:', err),
      });
    }

    this.addToast(`Post status changed to ${status.toUpperCase()}`, 'info');
  }

  deletePost(postId: string): void {
    this.posts.update((prev) => prev.filter((p) => p.id !== postId));

    if (postId.length > 20 && postId.includes('-') && !postId.startsWith('post-')) {
      this.postsApi.softDeletePost(postId).subscribe({
        next: () => {},
        error: (err: unknown) => console.warn('[BlogService] Backend delete failed:', err),
      });
    }

    this.addToast('Post moved to trash / deleted', 'warning');
  }

  // User Actions
  toggleUserStatus(userId: string): void {
    const targetUser = this.users().find((u) => u.id === userId);
    if (!targetUser) return;
    const nextStatus = targetUser.status === 'active' ? 'banned' : 'active';
    this.users.update((prev) =>
      prev.map((u) => (u.id === userId ? { ...u, status: nextStatus } : u))
    );
    this.addToast(`User ${targetUser.name} is now ${nextStatus}`, 'info');
  }

  deleteUser(userId: string): void {
    const targetUser = this.users().find((u) => u.id === userId);
    this.users.update((prev) => prev.filter((u) => u.id !== userId));

    const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(userId);
    if (isGuid) {
      this.usersApi.deleteUser(userId).subscribe({
        next: () => {},
        error: (err: unknown) => {
          console.warn('[BlogService] Backend delete user failed:', err);
          if (targetUser) {
            this.users.update((prev) => [...prev, targetUser]);
          }
          this.addToast('Failed to delete user on server', 'error');
        },
      });
    }

    this.addToast('User record removed', 'warning');
  }

  // Telemetry Actions
  loadTelemetry(days = 7): void {
    if (!this.isAdmin()) return;
    this.isTelemetryLoading.set(true);
    this.analyticsApi.get7DayTelemetry(days).subscribe({
      next: (summary) => {
        this.telemetrySummary.set({
          days: summary.days,
          totalViews7Days: summary.totalViews7Days,
          peakViews: summary.peakViews,
          totalComments7Days: summary.totalComments7Days,
          totalLikes7Days: summary.totalLikes7Days,
        });
        this.isTelemetryLoading.set(false);
      },
      error: (err) => {
        console.warn('[BlogService] Telemetry fetch failed:', err);
        this.isTelemetryLoading.set(false);
      },
    });
  }

  // Media Actions
  loadMedia(page = 1, pageSize = 24, query?: string): void {
    if (!this.isAdmin()) return;
    this.isMediaLoading.set(true);
    this.mediaApi.getStatus().subscribe({
      next: (status) => this.mediaStatus.set(status),
      error: () =>
        this.mediaStatus.set({
          configured: false,
          status: 'offline',
          maxFileSizeKb: 1024,
          allowedTypes: [],
        }),
    });

    this.mediaApi.getMedia(page, pageSize, query).subscribe({
      next: (res) => {
        const items: MediaItem[] = res.items.map((m) => ({
          id: m.id,
          public_id: m.publicId,
          url: m.url,
          filename: m.filename,
          mime_type: m.mimeType,
          file_size_kb: m.fileSizeKb,
          original_size_kb: m.fileSizeKb,
          optimized_size_kb: m.fileSizeKb,
          dimensions: m.dimensions,
          uploaded_at: m.createdAt,
          alt_text: m.altText || m.filename,
        }));
        this.media.set(items);
        this.isMediaLoading.set(false);
      },
      error: (err) => {
        console.warn('[BlogService] Media fetch failed:', err);
        this.isMediaLoading.set(false);
      },
    });
  }

  async uploadMedia(file: File | Blob, filename = 'asset.webp', altText?: string): Promise<MediaItem | null> {
    try {
      this.isMediaLoading.set(true);
      const res = await firstValueFrom(this.mediaApi.uploadMedia(file, filename, altText));
      const m = res.media;
      const newItem: MediaItem = {
        id: m.id,
        public_id: m.publicId,
        url: m.url,
        filename: m.filename,
        mime_type: m.mimeType,
        file_size_kb: m.fileSizeKb,
        original_size_kb: m.fileSizeKb,
        optimized_size_kb: m.fileSizeKb,
        dimensions: m.dimensions,
        uploaded_at: m.createdAt,
        alt_text: m.altText || m.filename,
      };
      this.media.update((prev) => [newItem, ...prev.filter((x) => x.id !== newItem.id)]);
      this.addToast(res.message || `Uploaded "${m.filename}" successfully`, 'success');
      return newItem;
    } catch (err: unknown) {
      const msg =
        (err as { error?: { detail?: string; message?: string } })?.error?.detail ||
        (err as { error?: { message?: string } })?.error?.message ||
        'Failed to upload image asset';
      this.addToast(msg, 'error');
      return null;
    } finally {
      this.isMediaLoading.set(false);
    }
  }

  async deleteMedia(id: string): Promise<boolean> {
    try {
      await firstValueFrom(this.mediaApi.deleteMedia(id));
      this.media.update((prev) => prev.filter((m) => m.id !== id));
      this.addToast('Media asset deleted', 'warning');
      return true;
    } catch (err) {
      console.warn('[BlogService] Delete media failed:', err);
      this.media.update((prev) => prev.filter((m) => m.id !== id));
      this.addToast('Removed from local view', 'warning');
      return false;
    }
  }

  // Profile & Site Settings Actions
  loadProfileFromBackend(): void {
    if (!this.isAdmin()) return;
    this.settingsApi.getSettings().subscribe({
      next: (settings) => {
        if (!settings) return;
        this.cloudinaryConfigured.set(settings.cloudinaryConfigured);
        let socials = this.profile().social_links;
        if (settings.socialLinksJson) {
          try {
            socials = JSON.parse(settings.socialLinksJson);
          } catch {}
        }
        this.profile.update((prev) => ({
          ...prev,
          name: settings.displayName || prev.name,
          email: settings.email || prev.email,
          bio: settings.bio || prev.bio,
          avatar_url: settings.avatarUrl || prev.avatar_url,
          role: settings.role || prev.role,
          tagline: settings.tagline || prev.tagline,
          location: settings.location || prev.location,
          banner_url: settings.bannerUrl || prev.banner_url,
          copyright_year: settings.copyrightYear || prev.copyright_year,
          social_links: socials,
        }));
      },
      error: (err) => {
        console.warn('[BlogService] Settings fetch failed, falling back to users API:', err);
        this.usersApi.getCurrentUser().subscribe({
          next: (user) => {
            if (!user) return;
            let socials = this.profile().social_links;
            if (user.information?.socialLinksJson) {
              try {
                socials = JSON.parse(user.information.socialLinksJson);
              } catch {}
            }
            this.profile.update((prev) => ({
              ...prev,
              name: user.displayName || user.username || prev.name,
              bio: user.bio || prev.bio,
              avatar_url: user.avatarUrl || prev.avatar_url,
              role: user.information?.jobTitle || prev.role,
              tagline: user.information?.tagline || prev.tagline,
              location: user.information?.location || prev.location,
              banner_url: user.information?.bannerUrl || prev.banner_url,
              copyright_year: user.information?.copyrightYear || prev.copyright_year,
              social_links: socials,
            }));
          },
          error: (err2) => console.warn('[BlogService] Current user fallback fetch failed:', err2),
        });
      },
    });
  }

  updateProfile(data: Partial<SiteProfile>): void {
    this.profile.update((prev) => ({ ...prev, ...data }));

    if (this.isAdmin()) {
      const socialJson = data.social_links ? JSON.stringify(data.social_links) : undefined;
      this.settingsApi
        .updateSettings({
          displayName: data.name,
          bio: data.bio,
          avatarUrl: data.avatar_url,
          role: data.role,
          tagline: data.tagline,
          location: data.location,
          bannerUrl: data.banner_url,
          copyrightYear: data.copyright_year,
          socialLinksJson: socialJson,
        })
        .subscribe({
          next: (res) => {
            this.cloudinaryConfigured.set(res.cloudinaryConfigured);
          },
          error: (err: unknown) => console.warn('[BlogService] Settings backend update failed:', err),
        });
    }

    this.addToast('Profile & Branding updated', 'success');
  }

  async uploadAvatar(file: File | Blob): Promise<string | null> {
    try {
      this.isMediaLoading.set(true);
      const res = await firstValueFrom(this.settingsApi.uploadAvatar(file));
      this.profile.update((prev) => ({ ...prev, avatar_url: res.url }));
      this.addToast('Avatar uploaded to Cloudinary CDN successfully', 'success');
      return res.url;
    } catch (err: unknown) {
      const msg =
        (err as { error?: { detail?: string } })?.error?.detail ||
        'Failed to upload avatar to Cloudinary CDN';
      this.addToast(msg, 'error');
      return null;
    } finally {
      this.isMediaLoading.set(false);
    }
  }

  async uploadBanner(file: File | Blob): Promise<string | null> {
    try {
      this.isMediaLoading.set(true);
      const res = await firstValueFrom(this.settingsApi.uploadBanner(file));
      this.profile.update((prev) => ({ ...prev, banner_url: res.url }));
      this.addToast('Banner uploaded to Cloudinary CDN successfully', 'success');
      return res.url;
    } catch (err: unknown) {
      const msg =
        (err as { error?: { detail?: string } })?.error?.detail ||
        'Failed to upload banner to Cloudinary CDN';
      this.addToast(msg, 'error');
      return null;
    } finally {
      this.isMediaLoading.set(false);
    }
  }

  // Auth Methods
  async loginAdmin(usernameOrPass: string, optionalPass?: string): Promise<boolean> {
    const email = optionalPass !== undefined ? usernameOrPass : this.config.authorEmail;
    const password = optionalPass !== undefined ? optionalPass : usernameOrPass;

    const result = await this.authService.signInWithPassword(email, password);
    if (result.success) {
      this.isAdmin.set(true);
      this.currentRoute.set('admin');
      this.loadAdminPosts();
      this.loadAdminComments();
      this.loadAdminUsers();
      this.router.navigate(['/admin'], {
        queryParams: { tab: this.adminTab() },
      });
      this.addToast('BIOS Administrator Session Initialized. Welcome, Derick.', 'success');
      return true;
    } else {
      this.addToast(result.error || 'Access Denied: Invalid Credentials', 'error');
      return false;
    }
  }

  async logoutAdmin(): Promise<void> {
    await this.authService.signOut();
    this.isAdmin.set(false);
    this.currentRoute.set('landing');
    this.router.navigate(['/']);
    this.addToast('Administrator session terminated.', 'info');
  }

  // Toast Notifications
  addToast(message: string, type: 'info' | 'success' | 'warning' | 'error' = 'info'): void {
    const id = `toast-${Date.now()}-${Math.random().toString(36).substring(2, 6)}`;
    this.toasts.update((prev) => [...prev, { id, message, type }]);
    setTimeout(() => {
      this.removeToast(id);
    }, 4000);
  }

  removeToast(id: string): void {
    this.toasts.update((prev) => prev.filter((t) => t.id !== id));
  }
}
