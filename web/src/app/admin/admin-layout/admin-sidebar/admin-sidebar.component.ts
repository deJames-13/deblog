import { Component, computed, inject } from '@angular/core';
import {
  LucideActivity,
  LucideExternalLink,
  LucideFileText,
  LucideImage,
  LucideLayoutDashboard,
  LucideLogOut,
  LucideMessageSquare,
  LucideSettings,
  LucideUsers,
} from '@lucide/angular';
import { BlogService } from '../../../core/services/blog.service';
import { AdminTab } from '../../../core/models/blog.model';
import { AuthorAvatarComponent } from '../../../common/author-avatar/author-avatar.component';

@Component({
  selector: 'app-admin-sidebar',
  imports: [
    AuthorAvatarComponent,
    LucideLayoutDashboard,
    LucideFileText,
    LucideUsers,
    LucideMessageSquare,
    LucideImage,
    LucideSettings,
    LucideExternalLink,
    LucideLogOut,
    LucideActivity,
  ],
  templateUrl: './admin-sidebar.component.html',
  styleUrl: './admin-sidebar.component.css',
})
export class AdminSidebarComponent {
  private readonly blogService = inject(BlogService);

  readonly adminTab = this.blogService.adminTab;
  readonly profile = this.blogService.profile;
  readonly posts = this.blogService.posts;
  readonly publishedPosts = this.blogService.publishedPosts;
  readonly draftPostsCount = this.blogService.draftPostsCount;
  readonly pendingCommentsCount = this.blogService.pendingCommentsCount;

  readonly totalViews = computed(() =>
    this.posts().reduce((sum, p) => sum + p.views_count, 0)
  );

  readonly adminNavItems = computed(() => {
    const drafts = this.draftPostsCount();
    const pending = this.pendingCommentsCount();

    return [
      {
        id: 'dashboard' as AdminTab,
        label: 'Dashboard',
        badge: null,
        badgeColor: '',
        desc: 'Metrics & KPI overview',
      },
      {
        id: 'posts' as AdminTab,
        label: 'Posts',
        badge: drafts > 0 ? `${drafts}` : null,
        badgeColor: 'bg-[var(--accent-terminal)] text-[var(--accent-terminal-contrast)]',
        desc: 'Manage & publish essays',
      },
      {
        id: 'users' as AdminTab,
        label: 'Users',
        badge: null,
        badgeColor: '',
        desc: 'Permissions & roles',
      },
      {
        id: 'comments' as AdminTab,
        label: 'Comments',
        badge: pending > 0 ? `${pending}` : null,
        badgeColor: 'bg-[var(--accent-orange)] text-[var(--accent-orange-contrast)]',
        desc: 'Moderation queue',
      },
      {
        id: 'media' as AdminTab,
        label: 'Media',
        badge: null,
        badgeColor: '',
        desc: 'Asset optimization',
      },
      {
        id: 'settings' as AdminTab,
        label: 'Settings',
        badge: null,
        badgeColor: '',
        desc: 'Profile & preferences',
      },
    ];
  });

  setAdminTab(tab: AdminTab): void {
    this.blogService.setAdminTab(tab);
  }

  navigateToLanding(): void {
    this.blogService.navigateTo('landing');
  }

  logout(): void {
    this.blogService.logoutAdmin();
  }
}
