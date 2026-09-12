import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideAlertTriangle,
  LucideBold,
  LucideCheckCircle,
  LucideCode,
  LucideItalic,
  LucideList,
  LucideMessageSquare,
  LucideQuote,
  LucideShieldCheck,
  LucideX,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { filterProfanity } from '../../core/data/profanity-list';
import { MarkdownRendererComponent } from '../../common/markdown-renderer/markdown-renderer.component';

@Component({
  selector: 'app-comment-modal',
  imports: [
    FormsModule,
    MarkdownRendererComponent,
    LucideMessageSquare,
    LucideX,
    LucideBold,
    LucideItalic,
    LucideCode,
    LucideQuote,
    LucideList,
    LucideAlertTriangle,
    LucideCheckCircle,
    LucideShieldCheck,
  ],
  templateUrl: './comment-modal.component.html',
})
export class CommentModalComponent {
  private readonly blogService = inject(BlogService);

  readonly isOpen = this.blogService.commentModalOpen;
  readonly posts = this.blogService.posts;
  readonly targetPostId = this.blogService.commentTargetPostId;

  readonly targetPost = computed(() => {
    const id = this.targetPostId();
    const list = this.posts();
    return list.find((p) => p.id === id) || list[0] || null;
  });

  readonly authorName = signal<string>('');
  readonly authorEmail = signal<string>('');
  readonly content = signal<string>('');
  readonly errorMsg = signal<string>('');
  readonly previewMode = signal<boolean>(false);

  readonly filterResult = computed(() => {
    return filterProfanity(this.content());
  });

  readonly isEmailValid = computed(() => {
    const email = this.authorEmail();
    return email.includes('@') && email.includes('.');
  });

  insertFormatting(prefix: string, suffix: string = ''): void {
    if (typeof document === 'undefined') return;
    const textarea = document.getElementById('comment-textarea') as HTMLTextAreaElement;
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const current = this.content();
    const selectedText = current.substring(start, end);
    const replacement = `${prefix}${selectedText || 'sample text'}${suffix}`;

    const newText = current.substring(0, start) + replacement + current.substring(end);
    this.content.set(newText);

    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(
        start + prefix.length,
        start + prefix.length + (selectedText.length || 11)
      );
    }, 10);
  }

  handleSubmit(event: Event): void {
    event.preventDefault();
    this.errorMsg.set('');

    if (!this.isEmailValid()) {
      this.errorMsg.set('Please provide a valid email address to verify humanity.');
      return;
    }

    if (this.filterResult().hasBadWords) {
      this.errorMsg.set(
        `Comment contains restricted language: "${this.filterResult().detectedWords.join(', ')}". Please edit your submission.`
      );
      return;
    }

    const post = this.targetPost();
    if (!post) return;

    const res = this.blogService.submitComment({
      postId: post.id,
      authorName: this.authorName().trim() || 'Anonymous Engineer',
      authorEmail: this.authorEmail().trim(),
      content: this.content().trim(),
    });

    if (!res.success) {
      this.errorMsg.set(res.message);
    } else {
      this.content.set('');
      this.authorName.set('');
      this.authorEmail.set('');
      this.errorMsg.set('');
      this.previewMode.set(false);
    }
  }

  close(): void {
    this.blogService.closeCommentModal();
  }
}
