import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { CommentsApiService } from './comments-api.service';

describe('CommentsApiService', () => {
  let service: CommentsApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        CommentsApiService,
      ],
    });

    service = TestBed.inject(CommentsApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get approved comments for a post', () => {
    service.getApprovedComments('post-123').subscribe((res) => {
      expect(res).toBeDefined();
    });

    const req = httpTesting.expectOne((r) => r.url.includes('/posts/post-123/comments'));
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should send X-Comment-Token header when updating comment with token', () => {
    service.updateComment('comm-1', 'updated text', 'token-abc-123').subscribe();

    const req = httpTesting.expectOne((r) => r.url.includes('/comments/comm-1'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.headers.get('X-Comment-Token')).toBe('token-abc-123');
    req.flush({ id: 'comm-1', content: 'updated text' });
  });
});
