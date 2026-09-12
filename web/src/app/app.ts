import { Component, computed, inject } from '@angular/core';
import { LucideMessageSquare, LucideMinimize2 } from '@lucide/angular';
import { BlogService } from './core/services/blog.service';
import { ToastComponent } from './common/toast/toast.component';
import { HelpModalComponent } from './common/help-modal/help-modal.component';
import { CommentModalComponent } from './guest/comment-modal/comment-modal.component';
import { TopbarComponent } from './common/topbar/topbar.component';
import { FooterComponent } from './common/footer/footer.component';
import { FKeyBarComponent } from './common/fkey-bar/fkey-bar.component';
import { SidebarComponent } from './guest/sidebar/sidebar.component';
import { LandingViewComponent } from './guest/landing/landing-view.component';
import { PostDetailViewComponent } from './guest/post-detail/post-detail-view.component';
import { ZenReaderComponent } from './guest/zen-reader/zen-reader.component';
import { AdminAuthComponent } from './admin/admin-auth/admin-auth.component';
import { AdminLayoutComponent } from './admin/admin-layout/admin-layout.component';

@Component({
  selector: 'app-root',
  imports: [
    ToastComponent,
    HelpModalComponent,
    CommentModalComponent,
    TopbarComponent,
    FooterComponent,
    FKeyBarComponent,
    SidebarComponent,
    LandingViewComponent,
    PostDetailViewComponent,
    ZenReaderComponent,
    AdminAuthComponent,
    AdminLayoutComponent,
    LucideMinimize2,
    LucideMessageSquare,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly blogService = inject(BlogService);

  readonly font = this.blogService.font;
  readonly zenMode = this.blogService.zenMode;
  readonly sidebarOpen = this.blogService.sidebarOpen;
  readonly currentRoute = this.blogService.currentRoute;
  readonly isAdmin = this.blogService.isAdmin;

  readonly fontClass = computed(() => `font-${this.font()}`);

  toggleZenMode(): void {
    this.blogService.toggleZenMode();
  }

  openCommentModal(): void {
    this.blogService.openCommentModal();
  }
}
