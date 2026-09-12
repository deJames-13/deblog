import { Component, inject } from '@angular/core';
import { BlogService } from '../../core/services/blog.service';
import { DashboardHomeComponent } from '../dashboard-home/dashboard-home.component';
import { AdminPostsComponent } from '../posts/admin-posts.component';
import { AdminUsersComponent } from '../users/admin-users.component';
import { AdminCommentsComponent } from '../comments/admin-comments.component';
import { AdminMediaComponent } from '../media/admin-media.component';
import { AdminSettingsComponent } from '../settings/admin-settings.component';
import { AdminSidebarComponent } from './admin-sidebar/admin-sidebar.component';

@Component({
  selector: 'app-admin-layout',
  imports: [
    AdminSidebarComponent,
    DashboardHomeComponent,
    AdminPostsComponent,
    AdminUsersComponent,
    AdminCommentsComponent,
    AdminMediaComponent,
    AdminSettingsComponent,
  ],
  templateUrl: './admin-layout.component.html',
})
export class AdminLayoutComponent {
  protected readonly blogService = inject(BlogService);
}
