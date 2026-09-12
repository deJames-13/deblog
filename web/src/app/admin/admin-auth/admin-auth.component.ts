import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideArrowLeft,
  LucideKey,
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
  ],
  templateUrl: './admin-auth.component.html',
})
export class AdminAuthComponent {
  private readonly blogService = inject(BlogService);

  readonly username = signal<string>('');
  readonly password = signal<string>('');
  readonly errorMsg = signal<string>('');

  handleLogin(e: Event): void {
    e.preventDefault();
    this.errorMsg.set('');
    const ok = this.blogService.loginAdmin(this.username(), this.password());
    if (!ok) {
      this.errorMsg.set('AUTHENTICATION_FAILED: Invalid username or security key.');
    }
  }

  goBack(): void {
    this.blogService.navigateTo('landing');
  }
}
