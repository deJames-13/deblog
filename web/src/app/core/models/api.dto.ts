/**
 * Backend API DTOs matching deblog ASP.NET Core 10 WebAPI contracts
 */

export interface AuthorSummaryDto {
  id: string;
  username: string;
  displayName?: string | null;
  avatarUrl?: string | null;
}

export interface PostAnalyticsDto {
  views: number;
  likes: number;
  shares: number;
  commentsCount: number;
}

export interface PostListItemDto {
  id: string;
  title: string;
  slug: string;
  summary?: string | null;
  content?: string;
  url: string;
  status: number | 'Draft' | 'Published' | 'Hidden' | 'Archived';
  isPublished: boolean;
  isDeleted: boolean;
  deletedAt?: string | null;
  publishedAt?: string | null;
  createdAt: string;
  author: AuthorSummaryDto;
  analytics: PostAnalyticsDto;
  coverImageUrl?: string | null;
  category?: string | null;
  tags?: string[];
  isFeatured?: boolean;
}

export interface PostDetailDto extends PostListItemDto {
  content: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreatePostRequest {
  title: string;
  slug?: string;
  summary?: string;
  content: string;
  status?: number | string;
  isPublished?: boolean;
  coverImageUrl?: string;
  category?: string;
  tags?: string[];
  isFeatured?: boolean;
}

export interface UpdatePostRequest {
  title?: string;
  slug?: string;
  summary?: string;
  content?: string;
  status?: number | string;
  isPublished?: boolean;
  coverImageUrl?: string;
  category?: string;
  tags?: string[];
  isFeatured?: boolean;
}

export interface CommentAuthorDto {
  id: string;
  username: string;
  displayName?: string | null;
  avatarUrl?: string | null;
}

export interface CommentResponseDto {
  id: string;
  postId: string;
  content: string;
  status: number | 'Pending' | 'Approved' | 'Rejected' | 'Spam';
  isDeleted: boolean;
  deletedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  author: CommentAuthorDto;
}

export interface CommentCreatedResponseDto {
  id: string;
  postId: string;
  content: string;
  status: number | string;
  managementToken: string;
  createdAt: string;
  author: CommentAuthorDto;
}

export interface AdminCommentResponseDto {
  id: string;
  postId: string;
  postTitle: string;
  content: string;
  status: number | string;
  isGuest: boolean;
  isDeleted: boolean;
  deletedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  author: CommentAuthorDto;
}

export interface CreateGuestCommentRequest {
  content: string;
  email: string;
  displayName?: string;
}

export interface UpdateCommentRequest {
  content: string;
  managementToken?: string;
}

export interface UserInformationDto {
  jobTitle?: string | null;
  tagline?: string | null;
  location?: string | null;
  bannerUrl?: string | null;
  copyrightYear?: string | null;
  socialLinksJson?: string | null;
}

export interface UserProfileDto {
  id: string;
  email: string;
  username: string;
  displayName?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  role: 'Admin' | 'User' | 'Guest' | string;
  status: number | 'Active' | 'Suspended' | 'Banned' | string;
  isDeleted: boolean;
  deletedAt?: string | null;
  createdAt: string;
  information?: UserInformationDto | null;
}

export interface UpdateUserProfileRequest {
  displayName?: string;
  bio?: string;
  avatarUrl?: string;
  jobTitle?: string;
  tagline?: string;
  location?: string;
  bannerUrl?: string;
  copyrightYear?: string;
  socialLinksJson?: string;
}

export interface AdminCreateUserRequest {
  email: string;
  username: string;
  displayName?: string;
  bio?: string;
  avatarUrl?: string;
  role?: string;
  status?: number | string;
}

export interface AdminUpdateUserRequest {
  email?: string;
  username?: string;
  displayName?: string;
  bio?: string;
  avatarUrl?: string;
  role?: string;
  status?: number | string;
}

export interface MediaItemDto {
  id: string;
  publicId: string;
  url: string;
  filename: string;
  mimeType: string;
  fileSizeBytes: number;
  fileSizeKb: number;
  width: number;
  height: number;
  dimensions: string;
  altText?: string | null;
  createdAt: string;
}

export interface UploadMediaResponseDto {
  media: MediaItemDto;
  message: string;
}

export interface MediaStatusDto {
  configured: boolean;
  status: 'online' | 'offline';
  maxFileSizeKb: number;
  allowedTypes: string[];
}

export interface DailyTelemetryDto {
  day: string;
  date: string;
  views: number;
  likes: number;
  shares: number;
  comments: number;
}

export interface TelemetrySummaryDto {
  days: DailyTelemetryDto[];
  totalViews7Days: number;
  peakViews: number;
  totalComments7Days: number;
  totalLikes7Days: number;
}

export interface SiteSettingsDto {
  userId: string;
  email: string;
  username: string;
  displayName: string;
  role?: string;
  tagline?: string;
  bio?: string;
  location?: string;
  avatarUrl?: string;
  bannerUrl?: string;
  copyrightYear?: string;
  socialLinksJson?: string;
  cloudinaryConfigured: boolean;
}

export interface UpdateSiteSettingsRequestDto {
  displayName?: string;
  role?: string;
  tagline?: string;
  bio?: string;
  location?: string;
  avatarUrl?: string;
  bannerUrl?: string;
  copyrightYear?: string;
  socialLinksJson?: string;
}

export interface UploadSettingAssetResponseDto {
  url: string;
  publicId: string;
  message: string;
}

