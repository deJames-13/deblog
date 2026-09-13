import { Component, computed, input, signal } from '@angular/core';

@Component({
  selector: 'app-author-avatar',
  template: `
    <div class="relative inline-flex shrink-0">
      @if (resolvedAvatarUrl() && !imageError()) {
        <img
          [src]="resolvedAvatarUrl()"
          [alt]="name() || 'Author'"
          (error)="imageError.set(true)"
          referrerpolicy="no-referrer"
          [class]="imgClass()"
        />
      } @else {
        <div
          [class]="fallbackClass()"
          [title]="name()"
          [attr.aria-label]="name()"
        >
          {{ initial() }}
        </div>
      }

      @if (showStatus()) {
        <span
          role="status"
          [attr.aria-label]="isOnline() ? 'Author is online' : 'Author is on standby'"
          [title]="isOnline() ? 'Online' : 'Standby / Offline'"
          [class]="statusDotClass()"
        ></span>
      }
    </div>
  `,
})
export class AuthorAvatarComponent {
  readonly name = input<string>('Author');
  readonly avatarUrl = input<string | undefined>(undefined);
  readonly size = input<'xs' | 'sm' | 'md' | 'lg'>('sm');
  readonly customClass = input<string>('');
  readonly showStatus = input<boolean>(false);
  readonly isOnline = input<boolean>(false);

  readonly imageError = signal<boolean>(false);

  readonly resolvedAvatarUrl = computed(() => {
    const raw = this.avatarUrl();
    if (!raw) return undefined;
    if (raw.startsWith('http://') || raw.startsWith('https://') || raw.startsWith('data:')) {
      return raw;
    }
    return raw.startsWith('/') ? raw : `/${raw}`;
  });

  readonly initial = computed(() => {
    const n = this.name()?.trim();
    return (n && n.length > 0 ? n[0] : 'D').toUpperCase();
  });

  private readonly sizeMap = {
    xs: 'w-4 h-4 text-[9px]',
    sm: 'w-5 h-5 text-[10px]',
    md: 'w-7 h-7 text-xs',
    lg: 'w-10 h-10 text-sm',
  };

  private readonly dotSizeMap = {
    xs: 'w-1.5 h-1.5',
    sm: 'w-2 h-2',
    md: 'w-2.5 h-2.5',
    lg: 'w-3 h-3',
  };

  readonly imgClass = computed(() => {
    const s = this.sizeMap[this.size()] || this.sizeMap.sm;
    return `${s} rounded-none object-cover border border-[var(--border-color)] shrink-0 ${this.customClass()}`;
  });

  readonly fallbackClass = computed(() => {
    const s = this.sizeMap[this.size()] || this.sizeMap.sm;
    return `${s} rounded-none bg-[var(--bg-canvas)] border border-[var(--border-color)] text-[var(--accent-terminal)] font-mono-bios font-bold flex items-center justify-center shrink-0 uppercase select-none ${this.customClass()}`;
  });

  readonly statusDotClass = computed(() => {
    const s = this.dotSizeMap[this.size()] || this.dotSizeMap.sm;
    const active = this.isOnline();
    const color = active
      ? 'bg-[var(--accent-terminal)] border-black animate-pulse'
      : 'bg-[var(--text-dim)] border-[var(--border-subtle)] opacity-70';
    return `absolute -bottom-0.5 -right-0.5 ${s} border ${color} transition-colors duration-200`;
  });
}
