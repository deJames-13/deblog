import { Component, OnInit, computed, inject, signal } from '@angular/core';
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
import { ConfirmDialogService } from '../../common/confirm-modal/confirm-modal.service';
import { TooltipDirective } from '../../common/tooltip/tooltip.directive';

@Component({
  selector: 'app-admin-comments',
  imports: [
    FormsModule,
    TooltipDirective,
    LucideTrash2,
    LucideAlertOctagon,
    LucideSearch,
    LucideCheckSquare,
    LucideSquare,
    LucideExternalLink,
  ],
  templateUrl: './admin-comments.component.html',
})
export class AdminCommentsComponent implements OnInit {
  private readonly blogService = inject(BlogService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly comments = this.blogService.comments;

  readonly activeTab = signal<'all' | CommentStatus>('pending');
  readonly searchQuery = signal<string>('');
  readonly selectedIds = signal<string[]>([]);

  ngOnInit(): void {
    this.blogService.loadAdminComments();
  }

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

  async batchAction(action: 'approve' | 'reject' | 'spam' | 'delete'): Promise<void> {
    const ids = this.selectedIds();
    if (ids.length === 0) return;

    if (action === 'delete') {
      const confirmed = await this.confirmDialog.confirm({
        title: 'BATCH_PURGE_COMMENTS',
        message: `Permanently remove ${ids.length} selected comment(s)?`,
        details: 'Selected comment records will be deleted from the database.',
        confirmText: 'DELETE COMMENTS',
        cancelText: 'CANCEL',
        tone: 'danger',
      });
      if (confirmed) {
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

  async deleteComment(id: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'DELETE_COMMENT_RECORD',
      message: 'Are you sure you want to delete this comment record?',
      confirmText: 'DELETE COMMENT',
      cancelText: 'CANCEL',
      tone: 'danger',
    });
    if (confirmed) {
      this.blogService.deleteComment(id);
    }
  }

  viewPost(postId: string): void {
    this.blogService.navigateTo('post', postId);
  }
}
