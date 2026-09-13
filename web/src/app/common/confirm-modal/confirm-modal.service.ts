import { Injectable, signal } from '@angular/core';

export interface ConfirmDialogOptions {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  tone?: 'danger' | 'warning' | 'info';
  details?: string;
}

export interface AlertDialogOptions {
  title: string;
  message: string;
  actionText?: string;
  tone?: 'danger' | 'warning' | 'info';
}

@Injectable({
  providedIn: 'root',
})
export class ConfirmDialogService {
  readonly isOpen = signal<boolean>(false);
  readonly title = signal<string>('CONFIRMATION_REQUIRED');
  readonly message = signal<string>('');
  readonly details = signal<string | null>(null);
  readonly confirmText = signal<string>('CONFIRM');
  readonly cancelText = signal<string>('CANCEL');
  readonly tone = signal<'danger' | 'warning' | 'info'>('warning');
  readonly isAlert = signal<boolean>(false);

  private resolver: ((confirmed: boolean) => void) | null = null;

  confirm(options: ConfirmDialogOptions): Promise<boolean> {
    this.title.set(options.title || 'CONFIRMATION_REQUIRED');
    this.message.set(options.message);
    this.details.set(options.details || null);
    this.confirmText.set(options.confirmText || 'CONFIRM');
    this.cancelText.set(options.cancelText || 'CANCEL');
    this.tone.set(options.tone || 'warning');
    this.isAlert.set(false);
    this.isOpen.set(true);

    return new Promise<boolean>((resolve) => {
      this.resolver = resolve;
    });
  }

  alert(options: AlertDialogOptions): Promise<void> {
    this.title.set(options.title || 'SYSTEM_NOTICE');
    this.message.set(options.message);
    this.details.set(null);
    this.confirmText.set(options.actionText || 'ACKNOWLEDGE');
    this.cancelText.set('DISMISS');
    this.tone.set(options.tone || 'info');
    this.isAlert.set(true);
    this.isOpen.set(true);

    return new Promise<void>((resolve) => {
      this.resolver = () => resolve();
    });
  }

  handleConfirm(): void {
    this.isOpen.set(false);
    if (this.resolver) {
      this.resolver(true);
      this.resolver = null;
    }
  }

  handleCancel(): void {
    this.isOpen.set(false);
    if (this.resolver) {
      this.resolver(false);
      this.resolver = null;
    }
  }
}
