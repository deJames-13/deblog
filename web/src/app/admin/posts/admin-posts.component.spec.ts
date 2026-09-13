import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AdminPostsComponent } from './admin-posts.component';
import { BlogService } from '../../core/services/blog.service';
import { ConfirmDialogService } from '../../common/confirm-modal/confirm-modal.service';

describe('AdminPostsComponent Fetch Lifecycle (TDD)', () => {
  let component: AdminPostsComponent;
  let fixture: ComponentFixture<AdminPostsComponent>;
  let loadAdminPostsSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    loadAdminPostsSpy = vi.fn().mockResolvedValue(undefined);

    const blogServiceStub = {
      posts: signal([]),
      media: signal([]),
      loadAdminPosts: loadAdminPostsSpy,
      createPost: vi.fn(),
      updatePost: vi.fn(),
      updatePostStatus: vi.fn(),
      deletePost: vi.fn(),
      navigateTo: vi.fn(),
    };

    const confirmDialogStub = {
      confirm: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [AdminPostsComponent],
      providers: [
        { provide: BlogService, useValue: blogServiceStub },
        { provide: ConfirmDialogService, useValue: confirmDialogStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminPostsComponent);
    component = fixture.componentInstance;
  });

  it('should call blogService.loadAdminPosts() on initialization', () => {
    fixture.detectChanges();
    expect(loadAdminPostsSpy).toHaveBeenCalledTimes(1);
  });
});
