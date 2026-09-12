import { Component, computed, inject } from '@angular/core';
import { BlogService } from '../../core/services/blog.service';
import { ThemeMode, TypographyFont } from '../../core/models/blog.model';

@Component({
  selector: 'app-fkey-bar',
  host: {
    '(window:keydown)': 'handleKeyDown($event)',
  },
  templateUrl: './fkey-bar.component.html',
})
export class FKeyBarComponent {
  private readonly blogService = inject(BlogService);

  readonly theme = this.blogService.theme;
  readonly font = this.blogService.font;
  readonly zenMode = this.blogService.zenMode;
  readonly currentRoute = this.blogService.currentRoute;
  readonly isAdmin = this.blogService.isAdmin;

  readonly keys = computed(() => {
    const isZen = this.zenMode();
    const route = this.currentRoute();
    const isAdm = this.isAdmin();
    const curTheme = this.theme();
    const curFont = this.font();

    return [
      {
        key: 'F1',
        label: 'HELP',
        active: false,
        action: () => this.blogService.openHelpModal(),
      },
      {
        key: 'F2',
        label: 'SEARCH',
        active: false,
        action: () => {
          this.focusSearch();
        },
      },
      {
        key: 'F3',
        label: isZen ? 'ZEN:ON' : 'ZEN',
        active: isZen,
        action: () => this.blogService.toggleZenMode(),
      },
      {
        key: 'F4',
        label: (curTheme === 'soft_light' ? 'LIGHT' : curTheme).toUpperCase(),
        active: false,
        action: () => this.cycleTheme(),
      },
      {
        key: 'F5',
        label: 'HOME',
        active: false,
        action: () => {
          this.blogService.navigateTo('landing');
          this.blogService.setSearchQuery('');
          this.blogService.setSelectedCategory(null);
          this.blogService.setSelectedMonth(null);
        },
      },
      {
        key: 'F6',
        label: curFont.toUpperCase(),
        active: false,
        action: () => this.cycleFont(),
      },
      {
        key: 'F7',
        label: 'TOP',
        active: false,
        action: () => {
          this.blogService.navigateTo('landing');
          this.blogService.setSelectedCategory('Top Articles');
        },
      },
      {
        key: 'F8',
        label: route === 'admin' ? 'EXIT' : 'ADMIN',
        active: route === 'admin' || route === 'admin-auth',
        action: () => {
          if (route === 'admin' || route === 'admin-auth') {
            this.blogService.navigateTo('landing');
          } else {
            this.blogService.navigateTo(isAdm ? 'admin' : 'admin-auth');
          }
        },
      },
      ...(route === 'post' || isZen
        ? [
            {
              key: 'ESC',
              label: 'BACK',
              active: false,
              action: () => {
                if (isZen) {
                  this.blogService.toggleZenMode();
                } else {
                  this.blogService.navigateTo('landing');
                }
              },
            },
          ]
        : []),
    ];
  });

  handleKeyDown(e: KeyboardEvent): void {
    const target = e.target as HTMLElement | null;
    const isInput =
      target &&
      (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable);

    if (e.key === 'F1') {
      e.preventDefault();
      this.blogService.openHelpModal();
    } else if (e.key === 'F2') {
      e.preventDefault();
      this.focusSearch();
    } else if (e.key === 'F3') {
      e.preventDefault();
      this.blogService.toggleZenMode();
    } else if (e.key === 'F4') {
      e.preventDefault();
      this.cycleTheme();
    } else if (e.key === 'F5') {
      e.preventDefault();
      this.blogService.navigateTo('landing');
      this.blogService.setSearchQuery('');
      this.blogService.setSelectedCategory(null);
      this.blogService.setSelectedMonth(null);
    } else if (e.key === 'F6') {
      e.preventDefault();
      this.cycleFont();
    } else if (e.key === 'F7') {
      e.preventDefault();
      this.blogService.navigateTo('landing');
      this.blogService.setSelectedCategory('Top Articles');
    } else if (e.key === 'F8') {
      e.preventDefault();
      const route = this.currentRoute();
      if (route === 'admin' || route === 'admin-auth') {
        this.blogService.navigateTo('landing');
      } else {
        this.blogService.navigateTo(this.isAdmin() ? 'admin' : 'admin-auth');
      }
    } else if (e.key === 'Escape') {
      if (this.blogService.helpModalOpen()) {
        e.preventDefault();
        this.blogService.closeHelpModal();
        return;
      }
      if (this.blogService.commentModalOpen()) {
        e.preventDefault();
        this.blogService.closeCommentModal();
        return;
      }
      if (this.zenMode()) {
        e.preventDefault();
        this.blogService.toggleZenMode();
      } else if (this.currentRoute() === 'post' || this.currentRoute() === 'search') {
        e.preventDefault();
        this.blogService.navigateTo('landing');
      }
    }
  }

  cycleTheme(): void {
    const themes: ThemeMode[] = ['night', 'twilight', 'sepia', 'light'];
    const current = this.theme();
    const currentNorm = current === 'soft_light' ? 'light' : current;
    const nextIndex = (themes.indexOf(currentNorm) + 1) % themes.length;
    this.blogService.setTheme(themes[nextIndex]);
  }

  cycleFont(): void {
    const fonts: TypographyFont[] = ['roboto', 'alice', 'noto', 'merriweather', 'comfortaa'];
    const current = this.font();
    const nextIndex = (fonts.indexOf(current) + 1) % fonts.length;
    this.blogService.setFont(fonts[nextIndex]);
  }

  focusSearch(): void {
    if (typeof document !== 'undefined') {
      const input =
        (document.getElementById('topbar-search-input') as HTMLInputElement) ||
        (document.getElementById('landing-search-input') as HTMLInputElement);
      if (input) {
        input.focus();
        input.select();
      } else {
        this.blogService.navigateTo('landing');
        setTimeout(() => {
          const lInput = document.getElementById('landing-search-input') as HTMLInputElement;
          lInput?.focus();
        }, 100);
      }
    }
  }
}
