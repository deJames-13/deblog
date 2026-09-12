import { Component, computed, inject } from '@angular/core';
import { BlogService } from '../../core/services/blog.service';
import { BlogPost } from '../../core/models/blog.model';
import { SearchFilterBarComponent } from './search-filter-bar/search-filter-bar.component';
import { PostCardComponent } from './post-card/post-card.component';
import { EmptyStateComponent } from './empty-state/empty-state.component';
import { PaginationComponent } from './pagination/pagination.component';

@Component({
  selector: 'app-landing-view',
  imports: [
    SearchFilterBarComponent,
    PostCardComponent,
    EmptyStateComponent,
    PaginationComponent,
  ],
  templateUrl: './landing-view.component.html',
})
export class LandingViewComponent {
  private readonly blogService = inject(BlogService);

  readonly posts = this.blogService.posts;
  readonly searchQuery = this.blogService.searchQuery;
  readonly selectedCategory = this.blogService.selectedCategory;
  readonly selectedMonth = this.blogService.selectedMonth;
  readonly currentPage = this.blogService.currentPage;
  readonly profile = this.blogService.profile;

  readonly postsPerPage = 4;

  readonly filteredPosts = computed(() => {
    let result = this.posts().filter((p) => p.status === 'published');

    // Category filter
    const cat = this.selectedCategory();
    if (cat) {
      if (cat === 'Top Articles') {
        result = [...result].sort((a, b) => b.likes_count - a.likes_count);
      } else {
        result = result.filter((p) => p.category.toLowerCase() === cat.toLowerCase());
      }
    }

    // Monthly archive filter
    const month = this.selectedMonth();
    if (month) {
      result = result.filter((p) => {
        const d = new Date(p.created_at);
        const mStr = d.toLocaleString('en-US', { month: 'long', year: 'numeric' });
        return mStr.toLowerCase() === month.toLowerCase();
      });
    }

    // Search query filter
    const q = this.searchQuery().toLowerCase().trim();
    if (q) {
      result = result.filter(
        (p) =>
          p.title.toLowerCase().includes(q) ||
          (p.subtitle && p.subtitle.toLowerCase().includes(q)) ||
          p.excerpt.toLowerCase().includes(q) ||
          p.tags.some((t) => t.toLowerCase().includes(q)) ||
          p.content.toLowerCase().includes(q)
      );
    }

    return result;
  });

  readonly hasActiveFilters = computed(() => {
    return !!this.selectedCategory() || !!this.selectedMonth() || !!this.searchQuery().trim();
  });

  readonly totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.filteredPosts().length / this.postsPerPage));
  });

  readonly safePage = computed(() => {
    return Math.min(Math.max(1, this.currentPage()), this.totalPages());
  });

  readonly displayedPosts = computed(() => {
    const page = this.safePage();
    const start = (page - 1) * this.postsPerPage;
    return this.filteredPosts().slice(start, start + this.postsPerPage);
  });

  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const arr: number[] = [];
    for (let i = 1; i <= total; i++) {
      arr.push(i);
    }
    return arr;
  });

  setSearch(val: string): void {
    this.blogService.setSearchQuery(val);
    this.blogService.setCurrentPage(1);
  }

  clearSearch(): void {
    this.blogService.setSearchQuery('');
    this.blogService.setCurrentPage(1);
  }

  clearAllFilters(): void {
    this.blogService.setSelectedCategory(null);
    this.blogService.setSelectedMonth(null);
    this.blogService.setSearchQuery('');
    this.blogService.setCurrentPage(1);
  }

  clearCategory(): void {
    this.blogService.setSelectedCategory(null);
  }

  clearMonth(): void {
    this.blogService.setSelectedMonth(null);
  }

  selectCategory(cat: string): void {
    this.blogService.setSelectedCategory(cat);
    this.blogService.setCurrentPage(1);
  }

  filterByTag(tag: string): void {
    this.blogService.setSearchQuery(tag);
    this.blogService.setCurrentPage(1);
  }

  goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages()) {
      this.blogService.setCurrentPage(p);
      if (typeof window !== 'undefined') {
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    }
  }

  viewPost(post: BlogPost): void {
    this.blogService.navigateTo('post', post.id);
  }

  hasLiked(postId: string): boolean {
    return this.blogService.hasLikedPost(postId);
  }

  toggleLike(postId: string): void {
    this.blogService.likePost(postId);
  }

  openComment(postId: string): void {
    this.blogService.openCommentModal(postId);
  }
}
