import {
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  LucideCalendar,
  LucideCheck,
  LucideClock,
  LucideEye,
  LucideHeart,
  LucideMessageSquare,
  LucideMinimize2,
  LucideShare2,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { AuthorAvatarComponent } from '../../common/author-avatar/author-avatar.component';
import { MarkdownRendererComponent } from '../../common/markdown-renderer/markdown-renderer.component';

@Component({
  selector: 'app-zen-reader',
  imports: [
    AuthorAvatarComponent,
    MarkdownRendererComponent,
    LucideMinimize2,
    LucideHeart,
    LucideShare2,
    LucideMessageSquare,
    LucideClock,
    LucideEye,
    LucideCalendar,
    LucideCheck,
  ],
  templateUrl: './zen-reader.component.html',
})
export class ZenReaderComponent {
  private readonly blogService = inject(BlogService);
  private readonly destroyRef = inject(DestroyRef);

  readonly activePost = this.blogService.activePost;
  readonly profile = this.blogService.profile;
  readonly font = this.blogService.font;

  readonly scrollProgress = signal<number>(0);
  readonly copied = signal<boolean>(false);
  protected readonly Math = Math;

  readonly isLiked = computed(() => {
    const post = this.activePost();
    return post ? this.blogService.hasLikedPost(post.id) : false;
  });

  readonly formattedDate = computed(() => {
    const post = this.activePost();
    if (!post) return '';
    return new Date(post.created_at).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  });

  constructor() {
    if (typeof window !== 'undefined') {
      const handleScroll = () => {
        const totalHeight = document.documentElement.scrollHeight - window.innerHeight;
        if (totalHeight > 0) {
          const progress = Math.min(100, Math.max(0, (window.scrollY / totalHeight) * 100));
          this.scrollProgress.set(progress);
        }
      };
      window.addEventListener('scroll', handleScroll);
      this.destroyRef.onDestroy(() => window.removeEventListener('scroll', handleScroll));
    }
  }

  toggleZen(): void {
    this.blogService.toggleZenMode();
  }

  toggleLike(): void {
    const post = this.activePost();
    if (post) {
      this.blogService.likePost(post.id);
    }
  }

  openComment(): void {
    const post = this.activePost();
    if (post) {
      this.blogService.openCommentModal(post.id);
    }
  }

  async handleShare(): Promise<void> {
    if (typeof navigator !== 'undefined' && navigator.clipboard) {
      try {
        await navigator.clipboard.writeText(window.location.href);
        this.copied.set(true);
        this.blogService.addToast('Article URL copied to clipboard!', 'success');
        setTimeout(() => this.copied.set(false), 3000);
      } catch {
        this.blogService.addToast('Failed to copy URL', 'error');
      }
    }
  }
}
