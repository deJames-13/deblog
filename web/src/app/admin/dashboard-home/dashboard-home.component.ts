import { Component, OnInit, computed, inject } from '@angular/core';
import {
  LucideAlertTriangle,
  LucideEye,
  LucideFileText,
  LucideHeart,
  LucideMessageSquare,
  LucidePieChart,
  LucideRefreshCw,
  LucideTrendingUp,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { AdminTab, CommentStatus } from '../../core/models/blog.model';
import { TooltipDirective } from '../../common/tooltip/tooltip.directive';

@Component({
  selector: 'app-dashboard-home',
  imports: [
    TooltipDirective,
    LucideFileText,
    LucideMessageSquare,
    LucideEye,
    LucideHeart,
    LucideTrendingUp,
    LucideAlertTriangle,
    LucidePieChart,
    LucideRefreshCw,
  ],
  templateUrl: './dashboard-home.component.html',
})
export class DashboardHomeComponent implements OnInit {
  protected readonly Math = Math;
  private readonly blogService = inject(BlogService);

  readonly posts = this.blogService.posts;
  readonly comments = this.blogService.comments;
  readonly users = this.blogService.users;
  readonly media = this.blogService.media;
  readonly telemetrySummary = this.blogService.telemetrySummary;
  readonly isTelemetryLoading = this.blogService.isTelemetryLoading;

  ngOnInit(): void {
    this.blogService.loadTelemetry(7);
  }

  // Publication Counts
  readonly publishedCount = computed(
    () => this.posts().filter((p) => p.status === 'published').length
  );
  readonly draftCount = computed(
    () => this.posts().filter((p) => p.status === 'draft').length
  );
  readonly hiddenCount = computed(
    () => this.posts().filter((p) => p.status === 'hidden' || p.status === 'archived').length
  );
  readonly totalPostsCount = computed(() => this.posts().length);

  readonly pendingComments = computed(
    () => this.comments().filter((c) => c.status === 'pending')
  );

  // Totals
  readonly totalViews = computed(() =>
    this.posts().reduce((sum, p) => sum + p.views_count, 0)
  );
  readonly totalLikes = computed(() =>
    this.posts().reduce((sum, p) => sum + p.likes_count, 0)
  );

  // Dynamic Realistic Engagement Score
  readonly engagementScore = computed(() => {
    const views = this.totalViews();
    if (views === 0) return '0.0';
    const interactions = this.totalLikes() * 2 + this.comments().length * 3;
    const ratio = (interactions / views) * 100;
    // Map onto a 1.0 - 5.0 rating scale
    const score = Math.min(5.0, Math.max(1.0, ratio * 0.4 + 2.0));
    return score.toFixed(1);
  });

  readonly engagementRatio = computed(() => {
    const views = this.totalViews();
    if (views === 0) return '0.0%';
    const totalInteractions = this.totalLikes() + this.comments().length;
    return `${((totalInteractions / views) * 100).toFixed(1)}%`;
  });

  // Post Distribution Donut Chart (Radius = 36, Circumference ≈ 226.2)
  readonly donutStats = computed(() => {
    const total = this.totalPostsCount();
    const circ = 226.2;
    if (total === 0) {
      return {
        pubPct: 0,
        dftPct: 0,
        hidPct: 0,
        pubDash: `0 ${circ}`,
        dftDash: `0 ${circ}`,
        hidDash: `0 ${circ}`,
        dftOffset: 0,
        hidOffset: 0,
      };
    }

    const pubPct = Math.round((this.publishedCount() / total) * 100);
    const dftPct = Math.round((this.draftCount() / total) * 100);
    const hidPct = Math.max(0, 100 - pubPct - dftPct);

    const pubLen = (pubPct / 100) * circ;
    const dftLen = (dftPct / 100) * circ;
    const hidLen = (hidPct / 100) * circ;

    return {
      pubPct,
      dftPct,
      hidPct,
      pubDash: `${pubLen} ${circ - pubLen}`,
      dftDash: `${dftLen} ${circ - dftLen}`,
      hidDash: `${hidLen} ${circ - hidLen}`,
      dftOffset: -pubLen,
      hidOffset: -(pubLen + dftLen),
    };
  });

  // Real 7-Day Telemetry from Database
  readonly telemetryDays = computed(() => {
    const summary = this.telemetrySummary();
    if (summary && summary.days.length > 0) {
      return summary.days;
    }
    const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    return days.map((d) => ({
      day: d,
      date: '',
      views: 0,
      likes: 0,
      shares: 0,
      comments: 0,
    }));
  });

  readonly peakViews = computed(() => {
    const summary = this.telemetrySummary();
    if (summary) return summary.peakViews;
    const max = Math.max(...this.telemetryDays().map((d) => d.views));
    return max > 0 ? max : 10;
  });

  readonly recentComments = computed(() => this.comments().slice(0, 4));
  readonly recentPosts = computed(() => this.posts().slice(0, 4));

  refreshTelemetry(): void {
    this.blogService.loadTelemetry(7);
  }

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
