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

@Component({
  selector: 'app-post-card',
  imports: [
    AuthorAvatarComponent,
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
