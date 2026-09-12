import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import {
  AdminTab,
  BlogComment,
  BlogPost,
  CommentStatus,
  MediaItem,
  PostStatus,
  SiteProfile,
  ThemeMode,
  ToastMessage,
  TypographyFont,
  UserAccount,
} from '../models/blog.model';
import {
  INITIAL_COMMENTS,
  INITIAL_MEDIA,
  INITIAL_POSTS,
  INITIAL_PROFILE,
  INITIAL_USERS,
} from '../data/mock-data';
import { filterProfanity } from '../data/profanity-list';

const LOCAL_STORAGE_KEYS = {
  THEME: 'bios_blog_theme',
  FONT: 'bios_blog_font',
  POSTS: 'bios_blog_posts',
  COMMENTS: 'bios_blog_comments',
  USERS: 'bios_blog_users',
  MEDIA: 'bios_blog_media',
  PROFILE: 'bios_blog_profile',
  LIKED: 'bios_blog_liked_ids',
  AUTH: 'bios_blog_is_admin',
};

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

@Injectable({
  providedIn: 'root',
})
export class BlogService {
  private readonly router = inject(Router);

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
  readonly currentRoute = signal<'landing' | 'post' | 'search' | 'admin-auth' | 'admin'>('landing');
  readonly selectedPostId = signal<string | null>(null);
  readonly adminTab = signal<AdminTab>('dashboard');

  // Search & Filter Signals
  readonly searchQuery = signal<string>('');
  readonly selectedCategory = signal<string | null>(null);
  readonly selectedMonth = signal<string | null>(null);
  readonly currentPage = signal<number>(1);

  // Auth Signal
  readonly isAdmin = signal<boolean>(
    typeof window !== 'undefined' ? localStorage.getItem(LOCAL_STORAGE_KEYS.AUTH) === 'true' : false
  );

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
    return list.find((p) => p.id === id) || list[0] || null;
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

    // Auto-save changes to localStorage
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

    effect(() => {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(LOCAL_STORAGE_KEYS.AUTH, this.isAdmin() ? 'true' : 'false');
      }
    });

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        const url = e.urlAfterRedirects || e.url;
        if (url.startsWith('/post/')) {
          const parts = url.split('/');
          const id = parts[2]?.split('?')[0]?.split('#')[0];
          if (id) {
            this.selectedPostId.set(id);
          }
          this.currentRoute.set('post');
        } else if (url.startsWith('/admin-auth')) {
          this.currentRoute.set('admin-auth');
        } else if (url.startsWith('/admin')) {
          if (!this.isAdmin()) {
            this.currentRoute.set('admin-auth');
          } else {
            this.currentRoute.set('admin');
          }
        } else if (url === '/' || url === '') {
          this.currentRoute.set('landing');
        }
      });
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
  navigateTo(route: 'landing' | 'post' | 'search' | 'admin-auth' | 'admin', postId?: string): void {
    if (route === 'post' && postId) {
      this.selectedPostId.set(postId);
      // Increment view count
      this.posts.update((prev) =>
        prev.map((p) => (p.id === postId ? { ...p, views_count: p.views_count + 1 } : p))
      );
      this.currentRoute.set('post');
      this.router.navigate(['/post', postId]);
    } else if (route === 'admin') {
      if (!this.isAdmin()) {
        this.currentRoute.set('admin-auth');
        this.router.navigate(['/admin-auth']);
        return;
      }
      this.currentRoute.set('admin');
      this.router.navigate(['/admin']);
    } else if (route === 'admin-auth') {
      this.currentRoute.set('admin-auth');
      this.router.navigate(['/admin-auth']);
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
    if (this.hasLikedPost(postId)) {
      this.likedPosts.update((prev) => prev.filter((id) => id !== postId));
      this.posts.update((prev) =>
        prev.map((p) => (p.id === postId ? { ...p, likes_count: Math.max(0, p.likes_count - 1) } : p))
      );
      this.addToast('Unliked post', 'info');
    } else {
      this.likedPosts.update((prev) => [...prev, postId]);
      this.posts.update((prev) =>
        prev.map((p) => (p.id === postId ? { ...p, likes_count: p.likes_count + 1 } : p))
      );
      this.addToast('Article Liked! Thank you.', 'success');
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

    const post = this.posts().find((p) => p.id === postId);
    const newComment: BlogComment = {
      id: `comm-${Date.now()}`,
      post_id: postId,
      post_title: post?.title || 'Personal Entry',
      author_name: authorName?.trim() || 'Anonymous Guest',
      author_email: authorEmail.trim(),
      content: content.trim(),
      status: 'pending',
      likes_count: 0,
      created_at: new Date().toISOString(),
    };

    this.comments.update((prev) => [newComment, ...prev]);

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
  }

  deleteComment(id: string): void {
    this.comments.update((prev) => prev.filter((c) => c.id !== id));
    this.addToast('Comment deleted', 'warning');
  }

  batchUpdateComments(ids: string[], status: CommentStatus): void {
    this.comments.update((prev) => prev.map((c) => (ids.includes(c.id) ? { ...c, status } : c)));
    this.addToast(`${ids.length} comments updated to ${status.toUpperCase()}`, 'success');
  }

  batchDeleteComments(ids: string[]): void {
    this.comments.update((prev) => prev.filter((c) => !ids.includes(c.id)));
    this.addToast(`${ids.length} comments permanently removed`, 'warning');
  }

  // Post CRUD Methods
  createPost(data: Partial<BlogPost>): BlogPost {
    const slug = (data.title || 'untitled-post')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/(^-|-$)+/g, '');

    const newPost: BlogPost = {
      id: `post-${Date.now()}`,
      slug,
      title: data.title || 'Untitled Post',
      subtitle: data.subtitle || '',
      excerpt:
        data.excerpt ||
        (data.content ? data.content.slice(0, 160) + '...' : 'No excerpt provided.'),
      content: data.content || '',
      cover_image:
        data.cover_image ||
        'https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=1000&q=80',
      category: data.category || 'General',
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
    this.addToast(`Post "${newPost.title}" created (${newPost.status})`, 'success');
    return newPost;
  }

  updatePost(postId: string, data: Partial<BlogPost>): void {
    this.posts.update((prev) =>
      prev.map((p) => {
        if (p.id === postId) {
          const updated = {
            ...p,
            ...data,
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
    this.addToast('Post updated successfully', 'success');
  }

  updatePostStatus(postId: string, status: PostStatus): void {
    this.posts.update((prev) =>
      prev.map((p) =>
        p.id === postId ? { ...p, status, updated_at: new Date().toISOString() } : p
      )
    );
    this.addToast(`Post status changed to ${status.toUpperCase()}`, 'info');
  }

  deletePost(postId: string): void {
    this.posts.update((prev) => prev.filter((p) => p.id !== postId));
    this.addToast('Post permanently deleted', 'warning');
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
    this.users.update((prev) => prev.filter((u) => u.id !== userId));
    this.addToast('User record removed', 'warning');
  }

  // Media Actions
  uploadMedia(file: {
    filename: string;
    url: string;
    file_size_kb: number;
    dimensions?: string;
    alt_text?: string;
  }): void {
    const original = file.file_size_kb;
    const optimized = Math.max(12, Math.round(original * 0.22));

    const item: MediaItem = {
      id: `med-${Date.now()}`,
      filename: file.filename,
      url: file.url,
      mime_type: 'image/webp',
      file_size_kb: optimized,
      original_size_kb: original,
      optimized_size_kb: optimized,
      dimensions: file.dimensions || '1920x1080',
      uploaded_at: new Date().toISOString(),
      alt_text: file.alt_text || file.filename.replace(/\.[^/.]+$/, ''),
    };

    this.media.update((prev) => [item, ...prev]);
    this.addToast(
      `Uploaded "${item.filename}" (Optimized ${original}KB -> ${optimized}KB WebP)`,
      'success'
    );
  }

  deleteMedia(id: string): void {
    this.media.update((prev) => prev.filter((m) => m.id !== id));
    this.addToast('Media asset removed', 'warning');
  }

  // Profile Actions
  updateProfile(data: Partial<SiteProfile>): void {
    this.profile.update((prev) => ({ ...prev, ...data }));
    this.addToast('Profile & Branding updated', 'success');
  }

  // Auth Methods
  loginAdmin(usernameOrPass: string, optionalPass?: string): boolean {
    let username = '';
    let password = '';

    if (optionalPass !== undefined) {
      username = usernameOrPass.trim();
      password = optionalPass.trim();
    } else {
      password = usernameOrPass.trim();
    }

    const cleanPass = password.toLowerCase();
    const cleanUser = username.toLowerCase();

    const isUserValid =
      optionalPass !== undefined
        ? cleanUser === 'admin' ||
          cleanUser === 'derick' ||
          cleanUser === 'drckespinosa.13@gmail.com' ||
          cleanUser.length > 0
        : true;

    const isPassValid =
      cleanPass === 'admin' ||
      cleanPass === 'bios2026' ||
      cleanPass === '1234' ||
      cleanPass === 'password';

    if (isUserValid && isPassValid) {
      this.isAdmin.set(true);
      this.currentRoute.set('admin');
      this.router.navigate(['/admin']);
      this.addToast('BIOS Administrator Session Initialized. Welcome, Derick.', 'success');
      return true;
    } else {
      this.addToast('Access Denied: Invalid Credentials', 'error');
      return false;
    }
  }

  logoutAdmin(): void {
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
