import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideArrowLeft,
  LucideKey,
  LucideLoader,
  LucideLock,
  LucideTerminal,
  LucideUser,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';

@Component({
  selector: 'app-admin-auth',
  imports: [
    FormsModule,
    LucideLock,
    LucideArrowLeft,
    LucideTerminal,
    LucideUser,
    LucideKey,
    LucideLoader,
  ],
  templateUrl: './admin-auth.component.html',
})
export class AdminAuthComponent {
  private readonly blogService = inject(BlogService);

  readonly username = signal<string>('');
  readonly password = signal<string>('');
  readonly errorMsg = signal<string>('');
  readonly isSubmitting = signal<boolean>(false);

  async handleLogin(e: Event): Promise<void> {
    e.preventDefault();
    if (this.isSubmitting()) return;

    this.errorMsg.set('');
    this.isSubmitting.set(true);

    try {
      const ok = await this.blogService.loginAdmin(this.username(), this.password());
      if (!ok) {
        this.errorMsg.set(
          this.blogService.authService.authError() ||
            'AUTHENTICATION_FAILED: Invalid credentials or unauthorized role.'
        );
      }
    } catch {
      this.errorMsg.set('CONNECTION_ERROR: Unable to contact authentication server.');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  goBack(): void {
    this.blogService.navigateTo('landing');
  }
}
