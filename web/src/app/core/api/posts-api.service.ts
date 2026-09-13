import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import {
  CreatePostRequest,
  PagedResult,
  PostDetailDto,
  PostListItemDto,
  UpdatePostRequest,
} from '../models/api.dto';

@Injectable({
  providedIn: 'root',
})
export class PostsApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG, { optional: true });
  private readonly baseUrl = `${this.config?.apiUrl || '/api'}/posts`;

  getPosts(options?: {
    page?: number;
    pageSize?: number;
    search?: string;
    category?: string;
    tag?: string;
    status?: number | string;
    publishedOnly?: boolean;
    authorId?: string;
  }): Observable<PagedResult<PostListItemDto>> {
    let params = new HttpParams();
    if (options?.page) params = params.set('page', options.page.toString());
    if (options?.pageSize) params = params.set('pageSize', options.pageSize.toString());
    if (options?.search) params = params.set('search', options.search);
    if (options?.category) params = params.set('category', options.category);
    if (options?.tag) params = params.set('tag', options.tag);
    if (options?.status !== undefined) params = params.set('status', options.status.toString());
    if (options?.publishedOnly !== undefined) params = params.set('publishedOnly', options.publishedOnly.toString());
    if (options?.authorId) params = params.set('authorId', options.authorId);

    return this.http.get<PagedResult<PostListItemDto>>(this.baseUrl, { params });
  }

  getPostByIdOrSlug(idOrSlug: string): Observable<PostDetailDto> {
    return this.http.get<PostDetailDto>(`${this.baseUrl}/${encodeURIComponent(idOrSlug)}`);
  }

  getTrashPosts(page = 1, pageSize = 20): Observable<PagedResult<PostListItemDto>> {
    const params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<PostListItemDto>>(`${this.baseUrl}/trash`, { params });
  }

  createPost(request: CreatePostRequest): Observable<PostDetailDto> {
    return this.http.post<PostDetailDto>(this.baseUrl, request);
  }

  updatePost(id: string, request: UpdatePostRequest): Observable<PostDetailDto> {
    return this.http.put<PostDetailDto>(`${this.baseUrl}/${id}`, request);
  }

  updatePostStatus(id: string, status: number | string): Observable<PostDetailDto> {
    return this.http.patch<PostDetailDto>(`${this.baseUrl}/${id}/status`, { status });
  }

  hidePost(id: string): Observable<PostDetailDto> {
    return this.http.post<PostDetailDto>(`${this.baseUrl}/${id}/hide`, {});
  }

  archivePost(id: string): Observable<PostDetailDto> {
    return this.http.post<PostDetailDto>(`${this.baseUrl}/${id}/archive`, {});
  }

  softDeletePost(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  restorePost(id: string): Observable<PostDetailDto> {
    return this.http.post<PostDetailDto>(`${this.baseUrl}/${id}/restore`, {});
  }

  forceDeletePost(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/force`);
  }

  trackView(idOrSlug: string): Observable<{ views: number }> {
    return this.http.post<{ views: number }>(`${this.baseUrl}/${encodeURIComponent(idOrSlug)}/analytics/view`, {});
  }

  trackLike(idOrSlug: string): Observable<{ likes: number }> {
    return this.http.post<{ likes: number }>(`${this.baseUrl}/${encodeURIComponent(idOrSlug)}/analytics/like`, {});
  }

  trackShare(idOrSlug: string): Observable<{ shares: number }> {
    return this.http.post<{ shares: number }>(`${this.baseUrl}/${encodeURIComponent(idOrSlug)}/analytics/share`, {});
  }
}
