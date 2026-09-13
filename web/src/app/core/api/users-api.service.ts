import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import {
  AdminCreateUserRequest,
  AdminUpdateUserRequest,
  PagedResult,
  UpdateUserProfileRequest,
  UserProfileDto,
} from '../models/api.dto';

@Injectable({
  providedIn: 'root',
})
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG, { optional: true });
  private readonly apiBase = this.config?.apiUrl || '/api';
  private readonly baseUrl = `${this.apiBase}/users`;
  private readonly adminUrl = `${this.apiBase}/admin/users`;

  getCurrentUser(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.baseUrl}/me`);
  }

  updateCurrentUser(request: UpdateUserProfileRequest): Observable<UserProfileDto> {
    return this.http.put<UserProfileDto>(`${this.baseUrl}/me`, request);
  }

  getUserById(id: string): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.baseUrl}/${id}`);
  }

  getAdminUsers(options?: {
    page?: number;
    pageSize?: number;
    search?: string;
    role?: string;
    status?: number | string;
  }): Observable<PagedResult<UserProfileDto>> {
    let params = new HttpParams();
    if (options?.page) params = params.set('page', options.page.toString());
    if (options?.pageSize) params = params.set('pageSize', options.pageSize.toString());
    if (options?.search) params = params.set('search', options.search);
    if (options?.role) params = params.set('role', options.role);
    if (options?.status !== undefined) params = params.set('status', options.status.toString());

    return this.http.get<PagedResult<UserProfileDto>>(this.adminUrl, { params });
  }

  getTrashUsers(page = 1, pageSize = 20): Observable<PagedResult<UserProfileDto>> {
    const params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<UserProfileDto>>(`${this.adminUrl}/trash`, { params });
  }

  createUser(request: AdminCreateUserRequest): Observable<UserProfileDto> {
    return this.http.post<UserProfileDto>(this.adminUrl, request);
  }

  updateUser(id: string, request: AdminUpdateUserRequest): Observable<UserProfileDto> {
    return this.http.put<UserProfileDto>(`${this.adminUrl}/${id}`, request);
  }

  updateUserStatus(id: string, status: number | string): Observable<UserProfileDto> {
    return this.http.patch<UserProfileDto>(`${this.adminUrl}/${id}/status`, { status });
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.adminUrl}/${id}`);
  }

  restoreUser(id: string): Observable<UserProfileDto> {
    return this.http.post<UserProfileDto>(`${this.adminUrl}/${id}/restore`, {});
  }

  forceDeleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.adminUrl}/${id}/force`);
  }
}
