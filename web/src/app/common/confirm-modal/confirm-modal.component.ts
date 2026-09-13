import { Component, ElementRef, inject, viewChild, effect } from '@angular/core';
import {
  LucideAlertOctagon,
  LucideAlertTriangle,
  LucideCheck,
  LucideInfo,
  LucideX,
} from '@lucide/angular';
import { ConfirmDialogService } from './confirm-modal.service';

@Component({
  selector: 'app-confirm-modal',
  host: {
    '(window:keydown.escape)': 'handleEscape()',
  },
  imports: [
    LucideAlertTriangle,
    LucideAlertOctagon,
    LucideInfo,
    LucideX,
    LucideCheck,
  ],
  templateUrl: './confirm-modal.component.html',
})
export class ConfirmModalComponent {
  readonly dialogService = inject(ConfirmDialogService);
  readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');
  readonly confirmButton = viewChild<ElementRef<HTMLButtonElement>>('confirmButton');

  constructor() {
    // When modal opens, focus the appropriate button for accessibility
    effect(() => {
      if (this.dialogService.isOpen()) {
        setTimeout(() => {
          if (this.dialogService.isAlert()) {
            this.confirmButton()?.nativeElement?.focus();
          } else {
            // Focus cancel button first for destructive safety
            this.cancelButton()?.nativeElement?.focus();
          }
        }, 50);
      }
    });
  }

  handleEscape(): void {
    if (this.dialogService.isOpen()) {
      this.dialogService.handleCancel();
    }
  }

  onConfirm(): void {
    this.dialogService.handleConfirm();
  }

  onCancel(): void {
    this.dialogService.handleCancel();
  }
}
