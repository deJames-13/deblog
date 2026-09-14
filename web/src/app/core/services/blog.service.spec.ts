import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { BlogService } from './blog.service';
import { PostsApiService } from '../api/posts-api.service';
import { CommentsApiService } from '../api/comments-api.service';
import { UsersApiService } from '../api/users-api.service';
import { SettingsApiService } from '../api/settings-api.service';
import { SupabaseAuthService } from '../auth/supabase-auth.service';
import { AdminCommentResponseDto, PagedResult, PostListItemDto, UserProfileDto } from '../models/api.dto';
import { UserAccount } from '../models/blog.model';

describe('BlogService Admin Fetching (TDD)', () => {
  let service: BlogService;
  let postsApiSpy: { getPosts: ReturnType<typeof vi.fn> };
  let commentsApiSpy: { getAdminComments: ReturnType<typeof vi.fn> };
  let usersApiSpy: { getAdminUsers: ReturnType<typeof vi.fn>; deleteUser: ReturnType<typeof vi.fn> };
  let settingsApiSpy: { getSettings: ReturnType<typeof vi.fn> };
  let authServiceStub: Partial<SupabaseAuthService>;

  beforeEach(() => {
    postsApiSpy = {
      getPosts: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 })),
    };
    commentsApiSpy = {
      getAdminComments: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 })),
    };
    usersApiSpy = {
      getAdminUsers: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 })),
      deleteUser: vi.fn().mockReturnValue(of(undefined)),
    };
    settingsApiSpy = {
      getSettings: vi.fn().mockReturnValue(of(null)),
    };
    authServiceStub = {
      isAdmin: signal(true),
      isAuthenticated: signal(true),
      token: signal('mock-jwt-token'),
      isCheckingAuth: signal(false),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        BlogService,
        { provide: PostsApiService, useValue: postsApiSpy },
        { provide: CommentsApiService, useValue: commentsApiSpy },
        { provide: UsersApiService, useValue: usersApiSpy },
        { provide: SettingsApiService, useValue: settingsApiSpy },
        { provide: SupabaseAuthService, useValue: authServiceStub },
      ],
    });

    service = TestBed.inject(BlogService);
  });

  it('loadAdminPosts() should fetch all posts with publishedOnly=false and update posts signal', async () => {
    const mockPostDto: PostListItemDto = {
      id: 'server-post-101',
      title: 'Draft Post From WebAPI Server',
      slug: 'draft-post-from-webapi-server',
      summary: 'Draft summary from backend',
      url: '/post/draft-post-from-webapi-server',
      status: 'Draft',
      isPublished: false,
      isDeleted: false,
      createdAt: '2026-09-13T12:00:00.000Z',
      author: { id: 'auth-1', username: 'derick', displayName: 'Derick' },
      analytics: { views: 42, likes: 7, shares: 1, commentsCount: 3 },
      category: 'Architecture',
      tags: ['Enterprise'],
      isFeatured: false,
    };

    const mockResult: PagedResult<PostListItemDto> = {
      items: [mockPostDto],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    };

    postsApiSpy.getPosts.mockReturnValue(of(mockResult));

    // Call public seam
    await service.loadAdminPosts();

    // Verify it called getPosts with publishedOnly: false
    expect(postsApiSpy.getPosts).toHaveBeenCalledWith(
      expect.objectContaining({ publishedOnly: false })
    );

    // Verify posts signal contains the mapped post from server
    const serverPost = service.posts().find((p) => p.id === 'server-post-101');
    expect(serverPost).toBeDefined();
    expect(serverPost?.title).toBe('Draft Post From WebAPI Server');
    expect(serverPost?.status).toBe('draft');
    expect(serverPost?.views_count).toBe(42);
    expect(serverPost?.likes_count).toBe(7);
  });

  it('loadAdminComments() should fetch comments from commentsApi and update comments signal', async () => {
    const mockCommentDto: AdminCommentResponseDto = {
      id: 'server-comm-201',
      postId: 'server-post-101',
      postTitle: 'Draft Post From WebAPI Server',
      content: 'This is a pending comment fetched from backend',
      status: 'Pending',
      isGuest: true,
      isDeleted: false,
      createdAt: '2026-09-13T13:00:00.000Z',
      updatedAt: '2026-09-13T13:00:00.000Z',
      author: { id: 'guest-1', username: 'alex', displayName: 'Alex Guest' },
    };

    const mockResult: PagedResult<AdminCommentResponseDto> = {
      items: [mockCommentDto],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    };

    commentsApiSpy.getAdminComments.mockReturnValue(of(mockResult));

    // Call public seam
    await service.loadAdminComments();

    expect(commentsApiSpy.getAdminComments).toHaveBeenCalled();

    // Verify comments signal has mapped comment
    const serverComment = service.comments().find((c) => c.id === 'server-comm-201');
    expect(serverComment).toBeDefined();
    expect(serverComment?.content).toBe('This is a pending comment fetched from backend');
    expect(serverComment?.status).toBe('pending');
    expect(serverComment?.author_name).toBe('Alex Guest');
    expect(serverComment?.post_title).toBe('Draft Post From WebAPI Server');
  });

  it('loadAdminUsers() should fetch users from usersApi and update users signal', async () => {
    const mockUserDto: UserProfileDto = {
      id: 'server-user-301',
      email: 'jane@enterprise.local',
      username: 'janedoe',
      displayName: 'Jane Doe',
      role: 'User',
      status: 'Active',
      isDeleted: false,
      createdAt: '2026-09-13T09:00:00.000Z',
    };

    const mockResult: PagedResult<UserProfileDto> = {
      items: [mockUserDto],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    };

    usersApiSpy.getAdminUsers.mockReturnValue(of(mockResult));

    // Call public seam
    await service.loadAdminUsers();

    expect(usersApiSpy.getAdminUsers).toHaveBeenCalled();

    // Verify users signal has mapped user
    const serverUser = service.users().find((u) => u.id === 'server-user-301');
    expect(serverUser).toBeDefined();
    expect(serverUser?.name).toBe('Jane Doe');
    expect(serverUser?.email).toBe('jane@enterprise.local');
    expect(serverUser?.role).toBe('commenter');
    expect(serverUser?.status).toBe('active');
  });

  it('syncFromBackend() should evict stale posts and set posts to empty array when all posts are deleted in database', async () => {
    // 1. Initial state has a post from backend
    const mockPostDto: PostListItemDto = {
      id: 'server-post-init',
      title: 'Active Post',
      slug: 'active-post',
      summary: 'Summary',
      url: '/post/active-post',
      status: 'Published',
      isPublished: true,
      isDeleted: false,
      createdAt: '2026-09-13T12:00:00.000Z',
      author: { id: 'auth-1', username: 'derick' },
      analytics: { views: 1, likes: 0, shares: 0, commentsCount: 0 },
      tags: [],
    };
    postsApiSpy.getPosts.mockReturnValue(of({
      items: [mockPostDto],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    }));

    await service.syncFromBackend();
    expect(service.posts().length).toBe(1);

    // 2. Data is deleted in Supabase dashboard -> backend returns 0 items
    const emptyResult: PagedResult<PostListItemDto> = {
      items: [],
      page: 1,
      pageSize: 50,
      totalCount: 0,
      totalPages: 0,
    };
    postsApiSpy.getPosts.mockReturnValue(of(emptyResult));

    // Act: run syncFromBackend
    await service.syncFromBackend();

    // Assert: posts() must be empty, reflecting the database deletion
    expect(service.posts()).toEqual([]);
    expect(service.lastSyncedAt()).toBeDefined();
  });

  it('startBackgroundSync() should periodically call syncFromBackend() and stopBackgroundSync() should cancel it', async () => {
    vi.useFakeTimers();
    const syncSpy = vi.spyOn(service, 'syncFromBackend').mockResolvedValue(false);

    (service as any).startBackgroundSync(5000);

    // Advance 5 seconds
    vi.advanceTimersByTime(5000);
    expect(syncSpy).toHaveBeenCalledTimes(1);

    // Advance another 5 seconds
    vi.advanceTimersByTime(5000);
    expect(syncSpy).toHaveBeenCalledTimes(2);

    // Stop background sync
    (service as any).stopBackgroundSync();
    vi.advanceTimersByTime(10000);
    expect(syncSpy).toHaveBeenCalledTimes(2);

    vi.useRealTimers();
  });

  it('syncFromBackend() should return false and not re-assign posts signal if remote data is identical', async () => {
    const mockPostDto: PostListItemDto = {
      id: 'server-post-same',
      title: 'Unchanged Article',
      slug: 'unchanged-article',
      summary: 'Summary',
      url: '/post/unchanged-article',
      status: 'Published',
      isPublished: true,
      isDeleted: false,
      createdAt: '2026-09-13T12:00:00.000Z',
      author: { id: 'auth-1', username: 'derick' },
      analytics: { views: 5, likes: 2, shares: 0, commentsCount: 0 },
      tags: [],
    };
    const mockResult: PagedResult<PostListItemDto> = {
      items: [mockPostDto],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    };
    postsApiSpy.getPosts.mockReturnValue(of(mockResult));

    // 1. Initial sync
    await service.syncFromBackend();
    const initialPostsRef = service.posts();
    expect(initialPostsRef.length).toBe(1);

    // 2. Background sync with IDENTICAL data
    const hasChanges = await (service as any).syncFromBackend({ silent: true });

    // Assert: Diff detection prevented re-assignment
    expect(hasChanges).toBe(false);
    expect(service.posts()).toBe(initialPostsRef); // Exact same object reference in signal
    expect(service.isLoading()).toBe(false);
  });

  it('syncFromBackend() should return true and re-assign posts signal silently when remote data has modified content or deleted items', async () => {
    const postA: PostListItemDto = {
      id: 'server-post-a',
      title: 'Original Title',
      slug: 'original-title',
      summary: 'Summary A',
      url: '/post/original-title',
      status: 'Published',
      isPublished: true,
      isDeleted: false,
      createdAt: '2026-09-13T12:00:00.000Z',
      author: { id: 'auth-1', username: 'derick' },
      analytics: { views: 5, likes: 2, shares: 0, commentsCount: 0 },
      tags: [],
    };
    postsApiSpy.getPosts.mockReturnValue(of({ items: [postA], page: 1, pageSize: 50, totalCount: 1, totalPages: 1 }));

    // 1. Initial sync
    await service.syncFromBackend();
    expect(service.posts()[0].title).toBe('Original Title');

    // 2. Post updated in Supabase dashboard (title changed)
    const postAModified: PostListItemDto = {
      ...postA,
      title: 'Modified Title in Supabase',
    };
    postsApiSpy.getPosts.mockReturnValue(of({ items: [postAModified], page: 1, pageSize: 50, totalCount: 1, totalPages: 1 }));

    // Act: silent background sync
    const hasChanges = await service.syncFromBackend({ silent: true });

    // Assert
    expect(hasChanges).toBe(true);
    expect(service.posts()[0].title).toBe('Modified Title in Supabase');
    expect(service.isLoading()).toBe(false); // Silent sync must never activate isLoading / skeleton
  });

  it('deleteUser() should call usersApi.deleteUser for GUID id and optimistically remove user from users signal', () => {
    const userId = '11111111-2222-3333-4444-555555555555';
    const testUser: UserAccount = {
      id: userId,
      name: 'Test Delete User',
      email: 'testdelete@example.com',
      role: 'commenter',
      status: 'active',
      comments_count: 0,
      created_at: new Date().toISOString(),
      last_active_at: new Date().toISOString(),
    };

    service.users.set([testUser]);
    expect(service.users().some((u) => u.id === userId)).toBe(true);

    // Act
    service.deleteUser(userId);

    // Assert: usersApi.deleteUser was called with the GUID
    expect(usersApiSpy.deleteUser).toHaveBeenCalledWith(userId);

    // Assert: user was removed optimistically from signal
    expect(service.users().some((u) => u.id === userId)).toBe(false);
  });

  it('deleteUser() should rollback user in users signal if backend returns error', () => {
    const userId = '11111111-2222-3333-4444-555555555555';
    const testUser: UserAccount = {
      id: userId,
      name: 'Test Delete User',
      email: 'testdelete@example.com',
      role: 'commenter',
      status: 'active',
      comments_count: 0,
      created_at: new Date().toISOString(),
      last_active_at: new Date().toISOString(),
    };

    usersApiSpy.deleteUser.mockReturnValue(throwError(() => new Error('Delete failed')));
    service.users.set([testUser]);

    // Act
    service.deleteUser(userId);

    // Assert: user is restored in users signal
    expect(service.users().some((u) => u.id === userId)).toBe(true);
  });

  it('loadProfileFromBackend() should execute in guest mode and update profile avatar_url and banner_url from settingsApi', () => {
    // Guest mode: isAdmin is false
    (authServiceStub.isAdmin as any).set(false);
    (authServiceStub.isAuthenticated as any).set(false);

    const mockSettings = {
      siteTitle: 'Deblog Site',
      tagline: 'Engineering Blog',
      copyrightYear: 2026,
      avatarUrl: 'https://res.cloudinary.com/demo/image/upload/v12345/new-avatar.png',
      bannerUrl: 'https://res.cloudinary.com/demo/image/upload/v12345/new-banner.png',
      displayName: 'Derick Espinosa',
      email: 'derick@example.com',
      bio: 'Fullstack Software Engineer',
      location: 'Manila, Philippines',
      socialLinksJson: JSON.stringify({ github: 'https://github.com/deJames-13' }),
      cloudinaryConfigured: true,
    };

    settingsApiSpy.getSettings.mockReturnValue(of(mockSettings));

    service.loadProfileFromBackend();

    expect(settingsApiSpy.getSettings).toHaveBeenCalled();
    expect(service.profile().avatar_url).toBe('https://res.cloudinary.com/demo/image/upload/v12345/new-avatar.png');
    expect(service.profile().banner_url).toBe('https://res.cloudinary.com/demo/image/upload/v12345/new-banner.png');
    expect(service.profile().name).toBe('Derick Espinosa');
    expect(service.profile().tagline).toBe('Engineering Blog');
    expect(service.cloudinaryConfigured()).toBe(true);
  });
});

