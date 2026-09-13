import { Component, computed, input, output } from '@angular/core';
import {
  LucideArrowRight,
  LucideClock,
  LucideEye,
  LucideHeart,
  LucideMessageSquare,
} from '@lucide/angular';
import { BlogPost } from '../../../core/models/blog.model';
import { AuthorAvatarComponent } from '../../../common/author-avatar/author-avatar.component';
import { MarkdownRendererComponent } from '../../../common/markdown-renderer/markdown-renderer.component';

@Component({
  selector: 'app-post-card',
  imports: [
    AuthorAvatarComponent,
    MarkdownRendererComponent,
    LucideClock,
    LucideEye,
    LucideHeart,
    LucideMessageSquare,
    LucideArrowRight,
  ],
  templateUrl: './post-card.component.html',
  styleUrl: './post-card.component.css',
})
export class PostCardComponent {
  readonly post = input.required<BlogPost>();
  readonly isLiked = input<boolean>(false);
  readonly authorName = input<string>('Chief Systems Architect');
  readonly authorAvatar = input<string>('');

  readonly view = output<BlogPost>();
  readonly categorySelect = output<string>();
  readonly tagSelect = output<string>();
  readonly toggleLike = output<string>();
  readonly openComment = output<string>();

  readonly formattedDate = computed(() => {
    return new Date(this.post().created_at).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  });

  /**
   * Shortened actual markdown content (first ~200-240 characters) for rich card preview
   */
  readonly previewMarkdown = computed(() => {
    const raw = this.post().content?.trim();
    if (!raw) {
      return this.post().excerpt || this.post().subtitle || '';
    }

    // Strip redundant leading heading if it repeats post title
    let text = raw;
    if (text.startsWith('# ') || text.startsWith('## ')) {
      const firstNewline = text.indexOf('\n');
      if (firstNewline !== -1) {
        text = text.slice(firstNewline).trim();
      }
    }

    // Extract first meaningful text paragraph (skip markdown image tags)
    const paragraphs = text
      .split(/\n\s*\n/)
      .map((p) => p.trim())
      .filter((p) => p.length > 0 && !p.startsWith('!['));
    const firstParagraph = paragraphs[0] || text;

    if (firstParagraph.length > 240) {
      const truncated = firstParagraph.slice(0, 240);
      const lastSpace = truncated.lastIndexOf(' ');
      return (lastSpace > 180 ? truncated.slice(0, lastSpace) : truncated) + '...';
    }

    return firstParagraph;
  });

  onCategoryClick(event: Event): void {
    event.stopPropagation();
    this.categorySelect.emit(this.post().category);
  }

  onTagClick(event: Event, tag: string): void {
    event.stopPropagation();
    this.tagSelect.emit(tag);
  }

  onLikeClick(event: Event): void {
    event.stopPropagation();
    this.toggleLike.emit(this.post().id);
  }

  onCommentClick(event: Event): void {
    event.stopPropagation();
    this.openComment.emit(this.post().id);
  }

  onReadClick(event: Event): void {
    event.stopPropagation();
    this.view.emit(this.post());
  }
}
