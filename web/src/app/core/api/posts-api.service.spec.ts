import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { PostsApiService } from './posts-api.service';
import { PagedResult, PostListItemDto } from '../models/api.dto';

describe('PostsApiService', () => {
  let service: PostsApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        PostsApiService,
      ],
    });

    service = TestBed.inject(PostsApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should query posts with pagination and search parameters', () => {
    const mockResult: PagedResult<PostListItemDto> = {
      items: [],
      page: 2,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    };

    service.getPosts({ page: 2, pageSize: 10, search: 'angular' }).subscribe((res) => {
      expect(res.page).toBe(2);
      expect(res.pageSize).toBe(10);
    });

    const req = httpTesting.expectOne((r) => r.url.includes('/posts') && r.params.get('page') === '2');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('search')).toBe('angular');

    req.flush(mockResult);
  });

  it('should trigger trackView POST request', () => {
    service.trackView('my-slug').subscribe((res) => {
      expect(res.views).toBe(1);
    });

    const req = httpTesting.expectOne((r) => r.url.includes('/analytics/view'));
    expect(req.request.method).toBe('POST');
    req.flush({ views: 1 });
  });
});
