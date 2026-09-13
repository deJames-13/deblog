import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { PostsApiService } from './posts-api.service';
import { CommentsApiService } from './comments-api.service';
import { UsersApiService } from './users-api.service';
import { authInterceptor } from '../auth/auth.interceptor';
import { SupabaseAuthService } from '../auth/supabase-auth.service';

describe('Admin API HTTP Contracts & Auth Interceptor (TDD)', () => {
  let httpTesting: HttpTestingController;
  let postsApi: PostsApiService;
  let commentsApi: CommentsApiService;
  let usersApi: UsersApiService;
  const mockToken = signal<string | null>('eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-admin-token');

  beforeEach(() => {
    const authStub: Partial<SupabaseAuthService> = {
      token: mockToken,
      isAdmin: signal(true),
      isAuthenticated: signal(true),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        PostsApiService,
        CommentsApiService,
        UsersApiService,
        { provide: SupabaseAuthService, useValue: authStub },
      ],
    });

    httpTesting = TestBed.inject(HttpTestingController);
    postsApi = TestBed.inject(PostsApiService);
    commentsApi = TestBed.inject(CommentsApiService);
    usersApi = TestBed.inject(UsersApiService);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should issue GET /api/posts?publishedOnly=false with Bearer token for admin posts fetch', () => {
    postsApi.getPosts({ page: 1, pageSize: 50, publishedOnly: false }).subscribe((res) => {
      expect(res.items.length).toBe(1);
    });

    const req = httpTesting.expectOne((r) => r.url.includes('/posts') && r.params.get('publishedOnly') === 'false');
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${mockToken()}`);

    req.flush({
      items: [{ id: 'p-1', title: 'Admin Post', slug: 'admin-post', isPublished: false }],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    });
  });

  it('should issue GET /api/admin/comments with Bearer token for admin comments moderation fetch', () => {
    commentsApi.getAdminComments({ page: 1, pageSize: 50 }).subscribe((res) => {
      expect(res.items.length).toBe(1);
    });

    const req = httpTesting.expectOne((r) => r.url.includes('/admin/comments') && r.params.get('page') === '1');
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${mockToken()}`);

    req.flush({
      items: [{ id: 'c-1', postId: 'p-1', postTitle: 'Post 1', content: 'Moderate me', status: 0 }],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    });
  });

  it('should issue GET /api/admin/users with Bearer token for admin user management fetch', () => {
    usersApi.getAdminUsers({ page: 1, pageSize: 50 }).subscribe((res) => {
      expect(res.items.length).toBe(1);
    });

    const req = httpTesting.expectOne((r) => r.url.includes('/admin/users') && r.params.get('page') === '1');
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${mockToken()}`);

    req.flush({
      items: [{ id: 'u-1', email: 'admin@test.com', username: 'admin', role: 'Admin', status: 0 }],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    });
  });
});
