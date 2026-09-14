import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideAlertTriangle,
  LucideCheck,
  LucideCopy,
  LucideHardDrive,
  LucideRefreshCw,
  LucideSearch,
  LucideTrash2,
  LucideUploadCloud,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { MediaItem } from '../../core/models/blog.model';
import { ConfirmDialogService } from '../../common/confirm-modal/confirm-modal.service';
import { TooltipDirective } from '../../common/tooltip/tooltip.directive';

@Component({
  selector: 'app-admin-media',
  imports: [
    FormsModule,
    TooltipDirective,
    LucideUploadCloud,
    LucideCopy,
    LucideTrash2,
    LucideCheck,
    LucideSearch,
    LucideAlertTriangle,
    LucideRefreshCw,
    LucideHardDrive,
  ],
  templateUrl: './admin-media.component.html',
})
export class AdminMediaComponent implements OnInit {
  private readonly blogService = inject(BlogService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly media = this.blogService.media;
  readonly mediaStatus = this.blogService.mediaStatus;
  readonly isMediaLoading = this.blogService.isMediaLoading;

  readonly isOffline = computed(() => {
    const status = this.mediaStatus();
    return status !== null && (!status.configured || status.status === 'offline');
  });

  readonly searchQuery = signal<string>('');
  readonly isDragging = signal<boolean>(false);
  readonly copiedId = signal<string | null>(null);
  readonly isUploading = signal<boolean>(false);

  readonly filteredMedia = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    return this.media().filter(
      (m) =>
        !q ||
        m.filename.toLowerCase().includes(q) ||
        m.alt_text?.toLowerCase().includes(q)
    );
  });

  readonly totalFilesCount = computed(() => this.media().length);
  readonly totalSizeKb = computed(() =>
    this.media().reduce((sum, m) => sum + (m.file_size_kb || 0), 0)
  );

  ngOnInit(): void {
    this.refreshMedia();
  }

  refreshMedia(): void {
    this.blogService.loadMedia(1, 50, this.searchQuery());
  }

  async handleFileDrop(e: DragEvent): Promise<void> {
    e.preventDefault();
    this.isDragging.set(false);

    if (e.dataTransfer?.files && e.dataTransfer.files.length > 0) {
      await this.processUploadFile(e.dataTransfer.files[0]);
    }
  }

  async handleFileInputChange(e: Event): Promise<void> {
    const input = e.target as HTMLInputElement;
    if (input?.files && input.files.length > 0) {
      await this.processUploadFile(input.files[0]);
      input.value = '';
    }
  }

  private async processUploadFile(file: File): Promise<void> {
    // 1. Validate MIME type
    if (!file.type.startsWith('image/')) {
      this.blogService.addToast('Only image files (JPEG, PNG, WebP, AVIF) are permitted.', 'error');
      return;
    }

    // 2. Strict 1MB size limit check (1024 * 1024 bytes)
    const maxSizeBytes = 1024 * 1024;
    if (file.size > maxSizeBytes) {
      const sizeKb = Math.round(file.size / 1024);
      this.blogService.addToast(
        `File size (${sizeKb} KB) exceeds the maximum allowed limit of 1MB (1024 KB). Upload rejected.`,
        'error'
      );
      return;
    }

    // 3. Upload to server/Cloudinary
    this.isUploading.set(true);
    try {
      await this.blogService.uploadMedia(file, file.name, file.name.replace(/\.[^/.]+$/, ''));
    } finally {
      this.isUploading.set(false);
    }
  }

  async copyMarkdown(item: MediaItem): Promise<void> {
    const md = `![${item.alt_text || item.filename}](${item.url})`;
    if (typeof navigator !== 'undefined' && navigator.clipboard) {
      try {
        await navigator.clipboard.writeText(md);
        this.copiedId.set(item.id);
        this.blogService.addToast('Markdown embed code copied to clipboard', 'success');
        setTimeout(() => this.copiedId.set(null), 2500);
      } catch {
        this.blogService.addToast('Failed to copy', 'error');
      }
    }
  }

  async deleteMedia(id: string, filename: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'DELETE_MEDIA_ASSET',
      message: `Are you sure you want to delete media asset "${filename}"?`,
      details: 'This file will be permanently removed from Cloudinary CDN and the database.',
      confirmText: 'DELETE ASSET',
      cancelText: 'CANCEL',
      tone: 'danger',
    });
    if (confirmed) {
      await this.blogService.deleteMedia(id);
    }
  }
}
