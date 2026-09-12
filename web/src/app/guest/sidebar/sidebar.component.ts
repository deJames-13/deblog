import { Component, computed, inject, output } from '@angular/core';
import {
  LucideCalendar,
  LucideClock,
  LucideFileText,
  LucideMail,
  LucideMapPin,
  LucideRadio,
  LucideTag,
  LucideTrendingUp,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { SocialIconComponent } from '../../common/social-icon/social-icon.component';

@Component({
  selector: 'app-sidebar',
  imports: [
    SocialIconComponent,
    LucideRadio,
    LucideMapPin,
    LucideMail,
    LucideFileText,
    LucideCalendar,
    LucideTrendingUp,
    LucideClock,
    LucideTag,
  ],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  private readonly blogService = inject(BlogService);

  readonly closeMobile = output<void>();

  readonly profile = this.blogService.profile;
  readonly posts = this.blogService.posts;
  readonly publishedPosts = this.blogService.publishedPosts;
  readonly selectedCategory = this.blogService.selectedCategory;
  readonly selectedMonth = this.blogService.selectedMonth;
  readonly sidebarOpen = this.blogService.sidebarOpen;

  readonly avatarUrl = computed(() => {
    const raw = this.profile().avatar_url;
    if (!raw) return '';
    if (raw.startsWith('http://') || raw.startsWith('https://') || raw.startsWith('data:')) {
      return raw;
    }
    return raw.startsWith('/') ? raw : `/${raw}`;
  });

  readonly categoryCounts = computed(() => {
    const counts: Record<string, number> = {};
    for (const p of this.publishedPosts()) {
      counts[p.category] = (counts[p.category] || 0) + 1;
    }
    return counts;
  });

  readonly categoriesList = computed(() => Object.keys(this.categoryCounts()));

  readonly monthCounts = computed(() => {
    const counts: Record<string, number> = {};
    for (const p of this.publishedPosts()) {
      const d = new Date(p.created_at);
      const key = d.toLocaleString('en-US', { month: 'long', year: 'numeric' });
      counts[key] = (counts[key] || 0) + 1;
    }
    return counts;
  });

  readonly monthsList = computed(() => Object.keys(this.monthCounts()));

  readonly topPosts = computed(() => {
    return [...this.publishedPosts()]
      .sort((a, b) => b.likes_count - a.likes_count)
      .slice(0, 3);
  });

  readonly latestPosts = computed(() => {
    return [...this.publishedPosts()]
      .sort(
        (a, b) => new Date(b.created_at).getTime() - new Date(a.created_at).getTime()
      )
      .slice(0, 3);
  });

  readonly allTags = computed(() => {
    const set = new Set<string>();
    for (const p of this.publishedPosts()) {
      for (const t of p.tags) {
        set.add(t);
      }
    }
    return Array.from(set);
  });

  selectCategory(cat: string): void {
    if (this.selectedCategory()?.toLowerCase() === cat.toLowerCase()) {
      this.blogService.setSelectedCategory(null);
    } else {
      this.blogService.setSelectedCategory(cat);
      this.blogService.setSelectedMonth(null);
      this.blogService.navigateTo('landing');
    }
  }

  selectMonth(month: string): void {
    if (this.selectedMonth()?.toLowerCase() === month.toLowerCase()) {
      this.blogService.setSelectedMonth(null);
    } else {
      this.blogService.setSelectedMonth(month);
      this.blogService.setSelectedCategory(null);
      this.blogService.navigateTo('landing');
    }
  }

  filterByTag(tag: string): void {
    this.blogService.setSearchQuery(tag);
    this.blogService.navigateTo('landing');
  }

  viewPost(postId: string): void {
    this.blogService.navigateTo('post', postId);
  }

  onCloseClick(): void {
    this.closeMobile.emit();
  }
}
