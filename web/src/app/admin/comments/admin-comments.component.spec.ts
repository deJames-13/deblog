import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AdminCommentsComponent } from './admin-comments.component';
import { BlogService } from '../../core/services/blog.service';
import { ConfirmDialogService } from '../../common/confirm-modal/confirm-modal.service';

describe('AdminCommentsComponent Fetch Lifecycle (TDD)', () => {
  let component: AdminCommentsComponent;
  let fixture: ComponentFixture<AdminCommentsComponent>;
  let loadAdminCommentsSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    loadAdminCommentsSpy = vi.fn().mockResolvedValue(undefined);

    const blogServiceStub = {
      comments: signal([]),
      loadAdminComments: loadAdminCommentsSpy,
      updateCommentStatus: vi.fn(),
      deleteComment: vi.fn(),
      batchUpdateComments: vi.fn(),
      batchDeleteComments: vi.fn(),
      navigateTo: vi.fn(),
    };

    const confirmDialogStub = {
      confirm: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [AdminCommentsComponent],
      providers: [
        { provide: BlogService, useValue: blogServiceStub },
        { provide: ConfirmDialogService, useValue: confirmDialogStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminCommentsComponent);
    component = fixture.componentInstance;
  });

  it('should call blogService.loadAdminComments() on initialization', () => {
    fixture.detectChanges();
    expect(loadAdminCommentsSpy).toHaveBeenCalledTimes(1);
  });
});
