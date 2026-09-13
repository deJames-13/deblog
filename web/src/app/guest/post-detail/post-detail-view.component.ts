import {
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  LucideAlertCircle,
  LucideArrowLeft,
  LucideCalendar,
  LucideCheck,
  LucideClock,
  LucideEye,
  LucideHeart,
  LucideMaximize2,
  LucideMessageSquare,
  LucideShare2,
  LucideShieldCheck,
  LucideTag,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { PresenceService } from '../../core/services/presence.service';
import { AuthorAvatarComponent } from '../../common/author-avatar/author-avatar.component';
import { MarkdownRendererComponent } from '../../common/markdown-renderer/markdown-renderer.component';

@Component({
  selector: 'app-post-detail-view',
  imports: [
    AuthorAvatarComponent,
    MarkdownRendererComponent,
    LucideHeart,
    LucideShare2,
    LucideMaximize2,
    LucideArrowLeft,
    LucideClock,
    LucideEye,
    LucideCalendar,
    LucideMessageSquare,
    LucideCheck,
    LucideTag,
    LucideAlertCircle,
  ],
  templateUrl: './post-detail-view.component.html',
})
export class PostDetailViewComponent {
  private readonly blogService = inject(BlogService);
  private readonly presenceService = inject(PresenceService);
  private readonly destroyRef = inject(DestroyRef);

  readonly isAuthorOnline = this.presenceService.isAuthorOnline;
  readonly activePost = this.blogService.activePost;
  readonly isDetailLoading = this.blogService.isDetailLoading;
  readonly comments = this.blogService.comments;
  readonly profile = this.blogService.profile;
  readonly font = this.blogService.font;

  readonly scrollProgress = signal<number>(0);
  readonly copied = signal<boolean>(false);

  readonly isLiked = computed(() => {
    const post = this.activePost();
    return post ? this.blogService.hasLikedPost(post.id) : false;
  });

  readonly postComments = computed(() => {
    const post = this.activePost();
    if (!post) return [];
    return this.comments().filter((c) => c.post_id === post.id);
  });

  readonly approvedComments = computed(() => {
    return this.postComments().filter((c) => c.status === 'approved');
  });

  readonly pendingComments = computed(() => {
    return this.postComments().filter((c) => c.status === 'pending');
  });

  readonly formattedDate = computed(() => {
    const post = this.activePost();
    if (!post) return '';
    return new Date(post.created_at).toLocaleDateString('en-US', {
      weekday: 'short',
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

  toggleZen(): void {
    this.blogService.toggleZenMode();
  }

  goBack(): void {
    this.blogService.navigateTo('landing');
  }

  async handleShare(): Promise<void> {
    const post = this.activePost();
    if (!post) return;

    const shareData = {
      title: post.title,
      text: post.excerpt,
      url: typeof window !== 'undefined' ? window.location.href : '',
    };

    if (typeof navigator !== 'undefined' && navigator.share) {
      try {
        await navigator.share(shareData);
        this.blogService.addToast('Shared successfully', 'success');
        return;
      } catch {
        // Fallback
      }
    }

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

  formatCommentDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
}
