import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideCrop,
  LucideImage,
  LucideRotateCcw,
  LucideSave,
  LucideShare2,
  LucideUser,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { SiteProfile } from '../../core/models/blog.model';
import { INITIAL_PROFILE } from '../../core/data/mock-data';
import { ImageCropModalComponent } from '../image-crop-modal/image-crop-modal.component';

@Component({
  selector: 'app-admin-settings',
  imports: [
    FormsModule,
    ImageCropModalComponent,
    LucideSave,
    LucideRotateCcw,
    LucideUser,
    LucideShare2,
    LucideImage,
    LucideCrop,
  ],
  templateUrl: './admin-settings.component.html',
})
export class AdminSettingsComponent {
  private readonly blogService = inject(BlogService);

  readonly profile = this.blogService.profile;

  // Local form state
  readonly formData = signal<SiteProfile>({ ...this.profile() });

  // Crop Modal state
  readonly cropModalOpen = signal<boolean>(false);
  readonly cropImageSrc = signal<string>('');
  readonly cropAspectRatio = signal<number>(1);
  readonly cropTitle = signal<string>('');
  readonly cropTargetField = signal<'avatar_url' | 'banner_url'>('avatar_url');

  constructor() {
    effect(() => {
      this.formData.set({ ...this.profile() });
    });
  }

  handleSave(e: Event): void {
    e.preventDefault();
    this.blogService.updateProfile(this.formData());
  }

  handleReset(): void {
    if (window.confirm('Reset profile to factory default settings?')) {
      this.formData.set({ ...INITIAL_PROFILE });
      this.blogService.updateProfile(INITIAL_PROFILE);
    }
  }

  handleFileSelect(event: Event, targetField: 'avatar_url' | 'banner_url'): void {
    const input = event.target as HTMLInputElement;
    const file = input?.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.blogService.addToast('Selected file must be an image', 'error');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const src = reader.result as string;
      this.cropImageSrc.set(src);
      this.cropAspectRatio.set(targetField === 'avatar_url' ? 1 : 3);
      this.cropTitle.set(
        targetField === 'avatar_url' ? 'Crop Avatar (1:1 Ratio)' : 'Crop Banner (3:1 Ratio)'
      );
      this.cropTargetField.set(targetField);
      this.cropModalOpen.set(true);
    };
    reader.readAsDataURL(file);
    input.value = '';
  }

  handleDrop(e: DragEvent, targetField: 'avatar_url' | 'banner_url'): void {
    e.preventDefault();
    const file = e.dataTransfer?.files?.[0];
    if (!file || !file.type.startsWith('image/')) return;

    const reader = new FileReader();
    reader.onload = () => {
      const src = reader.result as string;
      this.cropImageSrc.set(src);
      this.cropAspectRatio.set(targetField === 'avatar_url' ? 1 : 3);
      this.cropTitle.set(
        targetField === 'avatar_url' ? 'Crop Avatar (1:1 Ratio)' : 'Crop Banner (3:1 Ratio)'
      );
      this.cropTargetField.set(targetField);
      this.cropModalOpen.set(true);
    };
    reader.readAsDataURL(file);
  }

  openCropperForCurrent(targetField: 'avatar_url' | 'banner_url'): void {
    const currentUrl = this.formData()[targetField];
    if (!currentUrl) return;

    this.cropImageSrc.set(currentUrl);
    this.cropAspectRatio.set(targetField === 'avatar_url' ? 1 : 3);
    this.cropTitle.set(
      targetField === 'avatar_url' ? 'Crop Avatar (1:1 Ratio)' : 'Crop Banner (3:1 Ratio)'
    );
    this.cropTargetField.set(targetField);
    this.cropModalOpen.set(true);
  }

  handleCropComplete(dataUrl: string): void {
    const field = this.cropTargetField();
    this.formData.update((prev) => ({
      ...prev,
      [field]: dataUrl,
    }));
    this.cropModalOpen.set(false);
    this.blogService.addToast(
      `Updated ${field === 'avatar_url' ? 'Avatar' : 'Banner'} preview. Click Save to persist.`,
      'info'
    );
  }

  closeCropModal(): void {
    this.cropModalOpen.set(false);
  }
}
