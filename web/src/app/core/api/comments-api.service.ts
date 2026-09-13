import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import {
  AdminCommentResponseDto,
  CommentCreatedResponseDto,
  CommentResponseDto,
  CreateGuestCommentRequest,
  PagedResult,
  UpdateCommentRequest,
} from '../models/api.dto';

const GUEST_TOKEN_PREFIX = 'deblog_comment_token_';

@Injectable({
  providedIn: 'root',
})
export class CommentsApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG, { optional: true });
  private readonly apiBase = this.config?.apiUrl || '/api';

  getPostComments(postId: string): Observable<CommentResponseDto[]> {
    return this.http.get<CommentResponseDto[]>(`${this.apiBase}/posts/${postId}/comments`);
  }

  getApprovedComments(postId: string): Observable<CommentResponseDto[]> {
    return this.getPostComments(postId);
  }

  createComment(postId: string, request: CreateGuestCommentRequest): Observable<CommentCreatedResponseDto> {
    return this.http.post<CommentCreatedResponseDto>(`${this.apiBase}/posts/${postId}/comments`, request);
  }

  updateComment(commentId: string, content: string, managementToken?: string): Observable<CommentResponseDto> {
    const token = managementToken ?? this.getStoredCommentToken(commentId);
    let headers = new HttpHeaders();
    if (token) {
      headers = headers.set('X-Comment-Token', token);
    }
    const body: UpdateCommentRequest = { content, managementToken: token || undefined };
    return this.http.put<CommentResponseDto>(`${this.apiBase}/comments/${commentId}`, body, { headers });
  }

  deleteComment(commentId: string, managementToken?: string): Observable<void> {
    const token = managementToken ?? this.getStoredCommentToken(commentId);
    let headers = new HttpHeaders();
    if (token) {
      headers = headers.set('X-Comment-Token', token);
    }
    return this.http.delete<void>(`${this.apiBase}/comments/${commentId}`, { headers });
  }

  getAdminComments(options?: {
    page?: number;
    pageSize?: number;
    status?: number | string;
    postId?: string;
  }): Observable<PagedResult<AdminCommentResponseDto>> {
    let params = new HttpParams();
    if (options?.page) params = params.set('page', options.page.toString());
    if (options?.pageSize) params = params.set('pageSize', options.pageSize.toString());
    if (options?.status !== undefined) params = params.set('status', options.status.toString());
    if (options?.postId) params = params.set('postId', options.postId);

    return this.http.get<PagedResult<AdminCommentResponseDto>>(`${this.apiBase}/admin/comments`, { params });
  }

  getAdminTrashComments(page = 1, pageSize = 20): Observable<PagedResult<AdminCommentResponseDto>> {
    const params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<AdminCommentResponseDto>>(`${this.apiBase}/admin/comments/trash`, { params });
  }

  updateCommentStatus(id: string, status: number | string): Observable<AdminCommentResponseDto> {
    return this.http.patch<AdminCommentResponseDto>(`${this.apiBase}/admin/comments/${id}/status`, { status });
  }

  restoreComment(id: string): Observable<AdminCommentResponseDto> {
    return this.http.post<AdminCommentResponseDto>(`${this.apiBase}/admin/comments/${id}/restore`, {});
  }

  forceDeleteComment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/admin/comments/${id}/force`);
  }

  saveCommentToken(commentId: string, token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem(`${GUEST_TOKEN_PREFIX}${commentId}`, token);
    }
  }

  getStoredCommentToken(commentId: string): string | undefined {
    if (typeof window === 'undefined') return undefined;
    return localStorage.getItem(`${GUEST_TOKEN_PREFIX}${commentId}`) || undefined;
  }
}
