import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import {
  SiteSettingsDto,
  UpdateSiteSettingsRequestDto,
  UploadSettingAssetResponseDto,
} from '../models/api.dto';

@Injectable({
  providedIn: 'root',
})
export class SettingsApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG, { optional: true });
  private readonly baseUrl = `${this.config?.apiUrl || '/api'}/settings`;

  getSettings(): Observable<SiteSettingsDto> {
    return this.http.get<SiteSettingsDto>(this.baseUrl);
  }

  updateSettings(dto: UpdateSiteSettingsRequestDto): Observable<SiteSettingsDto> {
    return this.http.put<SiteSettingsDto>(this.baseUrl, dto);
  }

  uploadAvatar(file: File | Blob, filename = 'avatar.webp'): Observable<UploadSettingAssetResponseDto> {
    const formData = new FormData();
    if (file instanceof File) {
      formData.append('file', file, file.name);
    } else {
      formData.append('file', file, filename);
    }
    return this.http.post<UploadSettingAssetResponseDto>(`${this.baseUrl}/avatar`, formData);
  }

  uploadBanner(file: File | Blob, filename = 'banner.webp'): Observable<UploadSettingAssetResponseDto> {
    const formData = new FormData();
    if (file instanceof File) {
      formData.append('file', file, file.name);
    } else {
      formData.append('file', file, filename);
    }
    return this.http.post<UploadSettingAssetResponseDto>(`${this.baseUrl}/banner`, formData);
  }
}
