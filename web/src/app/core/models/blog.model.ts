/**
 * Schema types for deblog personal blogging platform
 */

export type PostStatus = 'published' | 'draft' | 'hidden' | 'archived';

export interface BlogPost {
  id: string;
  slug: string;
  title: string;
  subtitle?: string;
  excerpt: string;
  content: string;
  cover_image?: string;
  category: string;
  tags: string[];
  status: PostStatus;
  likes_count: number;
  views_count: number;
  reading_time_minutes: number;
  created_at: string;
  updated_at: string;
  published_at?: string;
  is_featured?: boolean;
}

export type CommentStatus = 'pending' | 'approved' | 'rejected' | 'spam';

export interface BlogComment {
  id: string;
  post_id: string;
  post_title?: string;
  author_name: string;
  author_email: string;
  content: string;
  status: CommentStatus;
  likes_count: number;
  created_at: string;
  flagged_bad_words?: string[];
}

export type UserRole = 'admin' | 'commenter' | 'subscriber';

export interface UserAccount {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  status: 'active' | 'banned' | 'flagged';
  comments_count: number;
  created_at: string;
  last_active_at: string;
}

export interface MediaItem {
  id: string;
  filename: string;
  url: string;
  mime_type: string;
  file_size_kb: number;
  original_size_kb?: number;
  optimized_size_kb?: number;
  dimensions: string;
  uploaded_at: string;
  alt_text: string;
  public_id?: string;
}

export interface DailyTelemetry {
  day: string;
  date: string;
  views: number;
  likes: number;
  shares: number;
  comments: number;
}

export interface TelemetrySummary {
  days: DailyTelemetry[];
  totalViews7Days: number;
  peakViews: number;
  totalComments7Days: number;
  totalLikes7Days: number;
}

export interface SiteProfile {
  name: string;
  role: string;
  tagline: string;
  bio: string;
  email: string;
  location: string;
  avatar_url: string;
  banner_url: string;
  copyright_year: string;
  social_links: {
    github: string;
    facebook: string;
    linkedin: string;
    instagram: string;
  };
}

export type ThemeMode = 'night' | 'twilight' | 'sepia' | 'light' | 'soft_light';
export type TypographyFont = 'roboto' | 'alice' | 'noto' | 'merriweather' | 'comfortaa';
export type AdminTab = 'dashboard' | 'home' | 'posts' | 'users' | 'comments' | 'media' | 'settings';

export interface FKeyAction {
  key: string;
  code: string;
  label: string;
  description: string;
  action: () => void;
}

export interface ToastMessage {
  id: string;
  type: 'info' | 'success' | 'warning' | 'error';
  message: string;
  durationMs?: number;
}
