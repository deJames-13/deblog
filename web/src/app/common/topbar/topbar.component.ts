import {
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  LucideExternalLink,
  LucideFileText,
  LucideHelpCircle,
  LucideImage,
  LucideLayoutDashboard,
  LucideLock,
  LucideLogOut,
  LucideMaximize2,
  LucideMessageSquare,
  LucideMinimize2,
  LucidePanelLeftClose,
  LucidePanelLeftOpen,
  LucideSearch,
  LucideSettings,
  LucideType,
  LucideUnlock,
  LucideUsers,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { AdminTab, ThemeMode, TypographyFont } from '../../core/models/blog.model';

@Component({
  selector: 'app-topbar',
  imports: [
    LucideSearch,
    LucideMaximize2,
    LucideMinimize2,
    LucidePanelLeftClose,
    LucidePanelLeftOpen,
    LucideLock,
    LucideUnlock,
    LucideType,
    LucideHelpCircle,
    LucideLayoutDashboard,
    LucideFileText,
    LucideUsers,
    LucideMessageSquare,
    LucideImage,
    LucideSettings,
    LucideLogOut,
    LucideExternalLink,
  ],
  templateUrl: './topbar.component.html',
})
export class TopbarComponent {
  private readonly blogService = inject(BlogService);
  private readonly destroyRef = inject(DestroyRef);

  readonly theme = this.blogService.theme;
  readonly font = this.blogService.font;
  readonly zenMode = this.blogService.zenMode;
  readonly sidebarOpen = this.blogService.sidebarOpen;
  readonly currentRoute = this.blogService.currentRoute;
  readonly searchQuery = this.blogService.searchQuery;
  readonly isAdmin = this.blogService.isAdmin;
  readonly adminTab = this.blogService.adminTab;
  readonly pendingCommentsCount = this.blogService.pendingCommentsCount;
  readonly draftPostsCount = this.blogService.draftPostsCount;

  readonly timeStr = signal<string>('');
  readonly showFontMenu = signal<boolean>(false);
  readonly showThemeMenu = signal<boolean>(false);

  readonly isDarkTheme = computed(() => {
    const t = this.theme();
    return t === 'night' || t === 'twilight';
  });

  readonly logoUrl = computed(() => {
    return this.isDarkTheme()
      ? '/assets/images/logo-transparent-dark.png'
      : '/assets/images/logo-transparent.png';
  });

  readonly themeOptions: { id: ThemeMode; label: string; hex: string }[] = [
    { id: 'night', label: 'Night', hex: '#3d3d3d' },
    { id: 'twilight', label: 'Twilight', hex: '#2c2b2b' },
    { id: 'sepia', label: 'Sepia', hex: '#fbf0d9' },
    { id: 'light', label: 'Light', hex: '#ffffff' },
  ];

  readonly fontOptions: { id: TypographyFont; name: string; sample: string }[] = [
    { id: 'roboto', name: 'Roboto', sample: 'Clean sans' },
    { id: 'alice', name: 'Alice', sample: 'Editorial serif' },
    { id: 'noto', name: 'Noto Serif', sample: 'Humanist serif' },
    { id: 'merriweather', name: 'Merriweather', sample: 'Warm reader serif' },
    { id: 'comfortaa', name: 'Comfortaa', sample: 'Soft geometric' },
  ];

  readonly adminNavItems = computed(() => {
    const drafts = this.draftPostsCount();
    const pending = this.pendingCommentsCount();

    return [
      { id: 'dashboard' as AdminTab, label: 'Dashboard', badge: null, badgeColor: '' },
      {
        id: 'posts' as AdminTab,
        label: 'Posts',
        badge: drafts > 0 ? `${drafts}` : null,
        badgeColor: '',
      },
      { id: 'users' as AdminTab, label: 'Users', badge: null, badgeColor: '' },
      {
        id: 'comments' as AdminTab,
        label: 'Comments',
        badge: pending > 0 ? `${pending}` : null,
        badgeColor: 'bg-[var(--accent-orange)] text-[var(--accent-orange-contrast)]',
      },
      { id: 'media' as AdminTab, label: 'Media', badge: null, badgeColor: '' },
      { id: 'settings' as AdminTab, label: 'Settings', badge: null, badgeColor: '' },
    ];
  });

  constructor() {
    this.updateClock();
    if (typeof window !== 'undefined') {
      const interval = setInterval(() => this.updateClock(), 1000);
      this.destroyRef.onDestroy(() => clearInterval(interval));
    }
  }

  private updateClock(): void {
    const now = new Date();
    this.timeStr.set(now.toISOString().replace('T', ' ').substring(0, 19));
  }

  setTheme(t: ThemeMode): void {
    this.blogService.setTheme(t);
    this.showThemeMenu.set(false);
  }

  setFont(f: TypographyFont): void {
    this.blogService.setFont(f);
    this.showFontMenu.set(false);
  }

  toggleThemeMenu(): void {
    this.showThemeMenu.update((v) => !v);
    this.showFontMenu.set(false);
  }

  toggleFontMenu(): void {
    this.showFontMenu.update((v) => !v);
    this.showThemeMenu.set(false);
  }

  toggleZenMode(): void {
    this.blogService.toggleZenMode();
  }

  toggleSidebar(): void {
    this.blogService.toggleSidebar();
  }

  openHelp(): void {
    this.blogService.openHelpModal();
  }

  onSearchChange(val: string): void {
    this.blogService.setSearchQuery(val);
    if (this.currentRoute() !== 'search' && this.currentRoute() !== 'landing') {
      this.blogService.navigateTo('landing');
    }
  }

  clearSearch(): void {
    this.blogService.setSearchQuery('');
  }

  navigateTo(route: 'landing' | 'post' | 'search' | 'admin-auth' | 'admin'): void {
    this.blogService.navigateTo(route);
  }

  setAdminTab(tab: AdminTab): void {
    this.blogService.setAdminTab(tab);
  }

  logoutAdmin(): void {
    this.blogService.logoutAdmin();
  }
}
