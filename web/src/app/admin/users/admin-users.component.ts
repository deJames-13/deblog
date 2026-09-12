import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideMail,
  LucideSearch,
  LucideTrash2,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { UserRole } from '../../core/models/blog.model';

@Component({
  selector: 'app-admin-users',
  imports: [
    FormsModule,
    LucideTrash2,
    LucideSearch,
    LucideMail,
  ],
  templateUrl: './admin-users.component.html',
})
export class AdminUsersComponent {
  private readonly blogService = inject(BlogService);

  readonly users = this.blogService.users;

  readonly searchQuery = signal<string>('');
  readonly roleFilter = signal<'all' | UserRole>('all');

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

  deleteUser(userId: string, userName: string): void {
    if (window.confirm(`Delete user record for "${userName}"?`)) {
      this.blogService.deleteUser(userId);
    }
  }
}
