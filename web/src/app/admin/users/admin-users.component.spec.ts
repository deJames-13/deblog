import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AdminUsersComponent } from './admin-users.component';
import { BlogService } from '../../core/services/blog.service';
import { ConfirmDialogService } from '../../common/confirm-modal/confirm-modal.service';

describe('AdminUsersComponent Fetch Lifecycle (TDD)', () => {
  let component: AdminUsersComponent;
  let fixture: ComponentFixture<AdminUsersComponent>;
  let loadAdminUsersSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    loadAdminUsersSpy = vi.fn().mockResolvedValue(undefined);

    const blogServiceStub = {
      users: signal([]),
      loadAdminUsers: loadAdminUsersSpy,
      toggleUserStatus: vi.fn(),
      deleteUser: vi.fn(),
    };

    const confirmDialogStub = {
      confirm: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [AdminUsersComponent],
      providers: [
        { provide: BlogService, useValue: blogServiceStub },
        { provide: ConfirmDialogService, useValue: confirmDialogStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUsersComponent);
    component = fixture.componentInstance;
  });

  it('should call blogService.loadAdminUsers() on initialization', () => {
    fixture.detectChanges();
    expect(loadAdminUsersSpy).toHaveBeenCalledTimes(1);
  });

  it('deleteUser() should call blogService.deleteUser(userId) when confirmed via dialog', async () => {
    const confirmService = TestBed.inject(ConfirmDialogService);
    const blogService = TestBed.inject(BlogService);
    vi.spyOn(confirmService, 'confirm').mockResolvedValue(true);

    await component.deleteUser('test-uuid-1', 'Alice');

    expect(confirmService.confirm).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'DELETE_USER_RECORD',
        tone: 'danger',
      })
    );
    expect(blogService.deleteUser).toHaveBeenCalledWith('test-uuid-1');
  });

  it('deleteUser() should not call blogService.deleteUser(userId) when dialog is cancelled', async () => {
    const confirmService = TestBed.inject(ConfirmDialogService);
    const blogService = TestBed.inject(BlogService);
    vi.spyOn(confirmService, 'confirm').mockResolvedValue(false);

    await component.deleteUser('test-uuid-2', 'Bob');

    expect(blogService.deleteUser).not.toHaveBeenCalled();
  });
});

