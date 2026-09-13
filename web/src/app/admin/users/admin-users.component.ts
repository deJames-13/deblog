import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideMail,
  LucideSearch,
  LucideTrash2,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { UserRole } from '../../core/models/blog.model';
import { ConfirmDialogService } from '../../common/confirm-modal/confirm-modal.service';
import { TooltipDirective } from '../../common/tooltip/tooltip.directive';

@Component({
  selector: 'app-admin-users',
  imports: [
    FormsModule,
    TooltipDirective,
    LucideTrash2,
    LucideSearch,
    LucideMail,
  ],
  templateUrl: './admin-users.component.html',
})
export class AdminUsersComponent implements OnInit {
  private readonly blogService = inject(BlogService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly users = this.blogService.users;

  readonly searchQuery = signal<string>('');
  readonly roleFilter = signal<'all' | UserRole>('all');

  ngOnInit(): void {
    this.blogService.loadAdminUsers();
  }

  readonly filteredUsers = computed(() => {
    const role = this.roleFilter();
    const q = this.searchQuery().toLowerCase().trim();

    return this.users().filter((u) => {
      const matchesRole = role === 'all' || u.role === role;
      const matchesQuery =
        !q ||
        u.name.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q);
      return matchesRole && matchesQuery;
    });
  });

  toggleStatus(userId: string): void {
    this.blogService.toggleUserStatus(userId);
  }

  async deleteUser(userId: string, userName: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'DELETE_USER_RECORD',
      message: `Delete user record for "${userName}"?`,
      details: 'This will remove the user account and associated permissions.',
      confirmText: 'DELETE USER',
      cancelText: 'CANCEL',
      tone: 'danger',
    });
    if (confirmed) {
      this.blogService.deleteUser(userId);
    }
  }
}
