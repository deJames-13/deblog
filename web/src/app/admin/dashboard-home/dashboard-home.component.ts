import { Component, computed, inject } from '@angular/core';
import {
  LucideAlertTriangle,
  LucideEye,
  LucideFileText,
  LucideHardDrive,
  LucideHeart,
  LucideMessageSquare,
  LucideTrendingUp,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { AdminTab, CommentStatus } from '../../core/models/blog.model';

@Component({
  selector: 'app-dashboard-home',
  imports: [
    LucideFileText,
    LucideMessageSquare,
    LucideEye,
    LucideHeart,
    LucideTrendingUp,
    LucideAlertTriangle,
    LucideHardDrive,
  ],
  templateUrl: './dashboard-home.component.html',
})
export class DashboardHomeComponent {
  protected readonly Math = Math;
  private readonly blogService = inject(BlogService);

  readonly posts = this.blogService.posts;
  readonly comments = this.blogService.comments;
  readonly users = this.blogService.users;
  readonly media = this.blogService.media;

  readonly publishedCount = computed(
    () => this.posts().filter((p) => p.status === 'published').length
  );
  readonly draftCount = computed(
    () => this.posts().filter((p) => p.status === 'draft').length
  );
  readonly pendingComments = computed(
    () => this.comments().filter((c) => c.status === 'pending')
  );

  readonly totalViews = computed(() =>
    this.posts().reduce((sum, p) => sum + p.views_count, 0)
  );
  readonly totalLikes = computed(() =>
    this.posts().reduce((sum, p) => sum + p.likes_count, 0)
  );

  readonly totalOriginalKb = computed(() =>
    this.media().reduce((sum, m) => sum + m.original_size_kb, 0)
  );
  readonly totalOptimizedKb = computed(() =>
    this.media().reduce((sum, m) => sum + m.optimized_size_kb, 0)
  );
  readonly spaceSavedKb = computed(() =>
    Math.max(0, this.totalOriginalKb() - this.totalOptimizedKb())
  );
  readonly spaceSavedPct = computed(() => {
    const orig = this.totalOriginalKb();
    return orig > 0 ? Math.round((this.spaceSavedKb() / orig) * 100) : 0;
  });

  readonly telemetryDays = [
    { day: 'Mon', views: 420, reads: 180, comments: 2 },
    { day: 'Tue', views: 580, reads: 260, comments: 4 },
    { day: 'Wed', views: 640, reads: 310, comments: 3 },
    { day: 'Thu', views: 890, reads: 420, comments: 7 },
    { day: 'Fri', views: 760, reads: 390, comments: 5 },
    { day: 'Sat', views: 530, reads: 220, comments: 1 },
    { day: 'Sun', views: 980, reads: 540, comments: 8 },
  ];

  readonly recentComments = computed(() => this.comments().slice(0, 4));
  readonly recentPosts = computed(() => this.posts().slice(0, 4));

  setAdminTab(tab: AdminTab): void {
    this.blogService.setAdminTab(tab);
  }

  updateCommentStatus(id: string, status: CommentStatus): void {
    this.blogService.updateCommentStatus(id, status);
  }

  viewPost(postId: string): void {
    this.blogService.navigateTo('post', postId);
  }
}
