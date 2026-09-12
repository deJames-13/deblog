import { Component, computed, inject } from '@angular/core';
import { UpperCasePipe } from '@angular/common';
import {
  LucideAlertCircle,
  LucideAlertTriangle,
  LucideCheckCircle2,
  LucideInfo,
  LucideLayers,
  LucideX,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';

@Component({
  selector: 'app-toast',
  imports: [
    UpperCasePipe,
    LucideCheckCircle2,
    LucideAlertTriangle,
    LucideAlertCircle,
    LucideInfo,
    LucideLayers,
    LucideX,
  ],
  templateUrl: './toast.component.html',
})
export class ToastComponent {
  private readonly blogService = inject(BlogService);

  readonly toasts = this.blogService.toasts;

  private readonly MAX_VISIBLE = 3;

  readonly visibleToasts = computed(() => {
    return this.toasts().slice(-this.MAX_VISIBLE);
  });

  readonly extraCount = computed(() => {
    return Math.max(0, this.toasts().length - this.MAX_VISIBLE);
  });

  removeToast(id: string): void {
    this.blogService.removeToast(id);
  }
}
