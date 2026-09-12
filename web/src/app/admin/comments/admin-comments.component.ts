import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideAlertOctagon,
  LucideCheckSquare,
  LucideExternalLink,
  LucideSearch,
  LucideSquare,
  LucideTrash2,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { CommentStatus } from '../../core/models/blog.model';

@Component({
  selector: 'app-admin-comments',
  imports: [
    FormsModule,
    LucideTrash2,
    LucideAlertOctagon,
    LucideSearch,
    LucideCheckSquare,
    LucideSquare,
    LucideExternalLink,
  ],
  templateUrl: './admin-comments.component.html',
})
export class AdminCommentsComponent {
  private readonly blogService = inject(BlogService);

  readonly comments = this.blogService.comments;

  readonly activeTab = signal<'all' | CommentStatus>('pending');
  readonly searchQuery = signal<string>('');
  readonly selectedIds = signal<string[]>([]);

  readonly filteredComments = computed(() => {
    const tab = this.activeTab();
    const q = this.searchQuery().toLowerCase().trim();

    return this.comments().filter((c) => {
      const matchesTab = tab === 'all' || c.status === tab;
      const matchesQuery =
        !q ||
        c.author_name.toLowerCase().includes(q) ||
        c.author_email.toLowerCase().includes(q) ||
        c.content.toLowerCase().includes(q) ||
        (c.post_title && c.post_title.toLowerCase().includes(q));
      return matchesTab && matchesQuery;
    });
  });

  readonly isAllSelected = computed(() => {
    const list = this.filteredComments();
    return list.length > 0 && this.selectedIds().length === list.length;
  });

  toggleSelectAll(): void {
    const list = this.filteredComments();
    if (this.isAllSelected()) {
      this.selectedIds.set([]);
    } else {
      this.selectedIds.set(list.map((c) => c.id));
    }
  }

  toggleSelect(id: string): void {
    this.selectedIds.update((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    );
  }

  isSelected(id: string): boolean {
    return this.selectedIds().includes(id);
  }

  batchAction(action: 'approve' | 'reject' | 'spam' | 'delete'): void {
    const ids = this.selectedIds();
    if (ids.length === 0) return;

    if (action === 'delete') {
      if (window.confirm(`Permanently remove ${ids.length} selected comment(s)?`)) {
        this.blogService.batchDeleteComments(ids);
        this.selectedIds.set([]);
      }
    } else {
      const statusMap: Record<string, CommentStatus> = {
        approve: 'approved',
        reject: 'rejected',
        spam: 'spam',
      };
      this.blogService.batchUpdateComments(ids, statusMap[action]);
      this.selectedIds.set([]);
    }
  }

  updateStatus(id: string, status: CommentStatus): void {
    this.blogService.updateCommentStatus(id, status);
  }

  deleteComment(id: string): void {
    if (window.confirm('Delete this comment record?')) {
      this.blogService.deleteComment(id);
    }
  }

  viewPost(postId: string): void {
    this.blogService.navigateTo('post', postId);
  }
}
