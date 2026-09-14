import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import {
  MediaItemDto,
  MediaStatusDto,
  PagedResult,
  UploadMediaResponseDto,
} from '../models/api.dto';

@Injectable({
  providedIn: 'root',
})
export class MediaApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG, { optional: true });
  private readonly baseUrl = `${this.config?.apiUrl || '/api'}/media`;

  getStatus(): Observable<MediaStatusDto> {
    return this.http.get<MediaStatusDto>(`${this.baseUrl}/status`);
  }

  getMedia(page = 1, pageSize = 24, query?: string): Observable<PagedResult<MediaItemDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (query) {
      params = params.set('query', query.trim());
    }
    return this.http.get<PagedResult<MediaItemDto>>(this.baseUrl, { params });
  }

  uploadMedia(file: File | Blob, filename = 'image.webp', altText?: string): Observable<UploadMediaResponseDto> {
    const formData = new FormData();
    if (file instanceof File) {
      formData.append('file', file, file.name);
    } else {
      formData.append('file', file, filename);
    }
    if (altText) {
      formData.append('altText', altText.trim());
    }
    return this.http.post<UploadMediaResponseDto>(`${this.baseUrl}/upload`, formData);
  }

  deleteMedia(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${id}`);
  }
}
