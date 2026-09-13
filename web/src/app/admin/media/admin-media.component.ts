import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideCheck,
  LucideCopy,
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
  ],
  templateUrl: './admin-media.component.html',
})
export class AdminMediaComponent {
  private readonly blogService = inject(BlogService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly media = this.blogService.media;

  readonly searchQuery = signal<string>('');
  readonly customUrl = signal<string>('');
  readonly customName = signal<string>('');
  readonly isDragging = signal<boolean>(false);
  readonly copiedId = signal<string | null>(null);

  readonly filteredMedia = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    return this.media().filter(
      (m) =>
        !q ||
        m.filename.toLowerCase().includes(q) ||
        m.alt_text.toLowerCase().includes(q)
    );
  });

  readonly totalOriginalKb = computed(() =>
    this.media().reduce((sum, m) => sum + m.original_size_kb, 0)
  );
  readonly totalOptimizedKb = computed(() =>
    this.media().reduce((sum, m) => sum + m.optimized_size_kb, 0)
  );
  readonly spaceSavedPct = computed(() => {
    const orig = this.totalOriginalKb();
    const saved = Math.max(0, orig - this.totalOptimizedKb());
    return orig > 0 ? Math.round((saved / orig) * 100) : 0;
  });

  handleManualUpload(e: Event): void {
    e.preventDefault();
    const url = this.customUrl().trim();
    if (!url) return;

    const name = this.customName().trim();
    const filename = name || url.split('/').pop()?.split('?')[0] || 'uploaded-asset.webp';
    const fakeOriginalSize = Math.floor(Math.random() * 400) + 200;

    this.blogService.uploadMedia({
      filename,
      url,
      file_size_kb: fakeOriginalSize,
      dimensions: '1920x1080',
      alt_text: name || 'Technical schematic figure',
    });

    this.customUrl.set('');
    this.customName.set('');
  }

  handleFileDrop(e: DragEvent): void {
    e.preventDefault();
    this.isDragging.set(false);

    if (e.dataTransfer?.files && e.dataTransfer.files.length > 0) {
      const file = e.dataTransfer.files[0];
      const objectUrl = URL.createObjectURL(file);
      const fakeSizeKb = Math.round(file.size / 1024) || 350;

      this.blogService.uploadMedia({
        filename: file.name,
        url: objectUrl,
        file_size_kb: fakeSizeKb,
        dimensions: '1920x1080',
        alt_text: file.name.replace(/\.[^/.]+$/, ''),
      });
    }
  }

  handleFileInputChange(e: Event): void {
    const input = e.target as HTMLInputElement;
    if (input?.files && input.files.length > 0) {
      const file = input.files[0];
      const objectUrl = URL.createObjectURL(file);
      const fakeSizeKb = Math.round(file.size / 1024) || 280;

      this.blogService.uploadMedia({
        filename: file.name,
        url: objectUrl,
        file_size_kb: fakeSizeKb,
        dimensions: '1920x1080',
        alt_text: file.name.replace(/\.[^/.]+$/, ''),
      });
      input.value = '';
    }
  }

  async copyMarkdown(item: MediaItem): Promise<void> {
    const md = `![${item.alt_text}](${item.url})`;
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
      details: 'This file will be permanently removed from storage and cannot be recovered.',
      confirmText: 'DELETE ASSET',
      cancelText: 'CANCEL',
      tone: 'danger',
    });
    if (confirmed) {
      this.blogService.deleteMedia(id);
    }
  }
}
