import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideBold,
  LucideCode,
  LucideEdit,
  LucideExternalLink,
  LucideEye,
  LucideEyeOff,
  LucideHeading2,
  LucideHeading3,
  LucideImage,
  LucideItalic,
  LucideList,
  LucidePlus,
  LucideQuote,
  LucideSearch,
  LucideTrash2,
  LucideX,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { BlogPost, PostStatus } from '../../core/models/blog.model';
import { MarkdownRendererComponent } from '../../common/markdown-renderer/markdown-renderer.component';
import { ConfirmDialogService } from '../../common/confirm-modal/confirm-modal.service';
import { TooltipDirective } from '../../common/tooltip/tooltip.directive';

@Component({
  selector: 'app-admin-posts',
  imports: [
    FormsModule,
    MarkdownRendererComponent,
    LucidePlus,
    LucideEdit,
    LucideTrash2,
    LucideEye,
    LucideEyeOff,
    LucideX,
    LucideSearch,
    LucideBold,
    LucideItalic,
    LucideCode,
    LucideQuote,
    LucideList,
    LucideHeading2,
    LucideHeading3,
    LucideImage,
    LucideExternalLink,
    TooltipDirective,
  ],
  templateUrl: './admin-posts.component.html',
})
export class AdminPostsComponent implements OnInit {
  private readonly blogService = inject(BlogService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly posts = this.blogService.posts;
  readonly media = this.blogService.media;

  readonly activeFilter = signal<'all' | PostStatus>('all');
  readonly searchQuery = signal<string>('');

  ngOnInit(): void {
    this.blogService.loadAdminPosts();
  }

  // Editor states
  readonly editingPost = signal<Partial<BlogPost> | null>(null);
  readonly isCreating = signal<boolean>(false);
  readonly editorPreview = signal<boolean>(false);
  readonly showMediaPicker = signal<boolean>(false);
  readonly tagsInput = signal<string>('');
  readonly isLoadingContent = signal<boolean>(false);

  readonly filteredPosts = computed(() => {
    const filter = this.activeFilter();
    const q = this.searchQuery().toLowerCase().trim();

    return this.posts().filter((p) => {
      const matchesFilter = filter === 'all' || p.status === filter;
      const matchesQuery =
        !q ||
        p.title.toLowerCase().includes(q) ||
        p.slug.toLowerCase().includes(q) ||
        p.category.toLowerCase().includes(q);
      return matchesFilter && matchesQuery;
    });
  });

  startCreate(): void {
    this.editingPost.set({
      title: '',
      subtitle: '',
      slug: '',
      content: '',
      excerpt: '',
      category: 'Power Platform',
      tags: ['Enterprise', 'Architecture'],
      status: 'draft',
      cover_image: '',
      is_featured: false,
    });
    this.tagsInput.set('Enterprise, Architecture');
    this.isCreating.set(true);
    this.editorPreview.set(false);
    this.isLoadingContent.set(false);
  }

  startEdit(post: BlogPost): void {
    this.editingPost.set({ ...post });
    this.tagsInput.set((post.tags || []).join(', '));
    this.isCreating.set(false);
    this.editorPreview.set(false);

    // Fetch full post markdown content and details from backend
    if (post.id) {
      this.isLoadingContent.set(true);
      this.blogService
        .fetchPostDetail(post.id)
        .then((fullPost) => {
          if (fullPost && this.editingPost()?.id === post.id) {
            this.editingPost.update((current) =>
              current
                ? {
                    ...current,
                    content: fullPost.content,
                    subtitle: fullPost.subtitle || current.subtitle,
                    excerpt: fullPost.excerpt || current.excerpt,
                  }
                : null
            );
          }
        })
        .finally(() => {
          this.isLoadingContent.set(false);
        });
    }
  }

  cancelEdit(): void {
    this.editingPost.set(null);
    this.isCreating.set(false);
    this.editorPreview.set(false);
    this.isLoadingContent.set(false);
  }

  savePost(publishImmediately: boolean = false): void {
    const post = this.editingPost();
    if (!post || !post.title) return;

    const tags = this.tagsInput()
      .split(',')
      .map((t) => t.trim())
      .filter((t) => t.length > 0);

    const finalStatus: PostStatus = publishImmediately
      ? 'published'
      : post.status || 'draft';

    if (this.isCreating()) {
      this.blogService.createPost({
        ...post,
        tags,
        status: finalStatus,
      });
    } else if (post.id) {
      this.blogService.updatePost(post.id, {
        ...post,
        tags,
        status: finalStatus,
      });
    }

    this.editingPost.set(null);
    this.isCreating.set(false);
  }

  insertMarkdown(prefix: string, suffix: string = ''): void {
    if (typeof document === 'undefined') return;
    const textarea = document.getElementById('post-content-editor') as HTMLTextAreaElement;
    if (!textarea) return;

    const post = this.editingPost();
    if (!post) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const current = post.content || '';
    const selected = current.substring(start, end);
    const replacement = `${prefix}${selected || 'text'}${suffix}`;

    const newContent = current.substring(0, start) + replacement + current.substring(end);
    this.editingPost.update((p) => (p ? { ...p, content: newContent } : null));

    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(
        start + prefix.length,
        start + prefix.length + (selected.length || 4)
      );
    }, 10);
  }

  selectMediaForCover(url: string): void {
    this.editingPost.update((p) => (p ? { ...p, cover_image: url } : null));
    this.showMediaPicker.set(false);
  }

  updatePostStatus(id: string, status: PostStatus): void {
    this.blogService.updatePostStatus(id, status);
  }

  async deletePost(id: string, title: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'PURGE_POST_RECORD',
      message: `Are you sure you want to permanently delete post "${title}"?`,
      details: 'This action will move the post to trash and remove it from public indexing.',
      confirmText: 'DELETE POST',
      cancelText: 'CANCEL',
      tone: 'danger',
    });
    if (confirmed) {
      this.blogService.deletePost(id);
    }
  }

  viewLive(postId: string): void {
    this.blogService.navigateTo('post', postId);
  }
}
