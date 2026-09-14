import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SidebarComponent } from './sidebar.component';
import { BlogService } from '../../core/services/blog.service';
import { PresenceService } from '../../core/services/presence.service';
import { signal } from '@angular/core';
import { SiteProfile, BlogPost } from '../../core/models/blog.model';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;
  let loadProfileSpy: ReturnType<typeof vi.fn>;

  const mockProfile = signal<SiteProfile>({
    name: 'Derick Espinosa',
    role: 'Power Platform Developer',
    tagline: 'Engineering robust automations',
    bio: 'BIOS minimalist architecture',
    email: 'drckespinosa.13@gmail.com',
    location: 'Manila, Philippines',
    avatar_url: '/assets/images/me.png',
    banner_url: 'https://example.com/banner.png',
    copyright_year: '2026',
    social_links: {
      github: 'https://github.com/deJames-13',
      facebook: '',
      linkedin: '',
      instagram: '',
    },
  });

  beforeEach(async () => {
    loadProfileSpy = vi.fn();

    const mockBlogService = {
      profile: mockProfile,
      posts: signal<BlogPost[]>([]),
      publishedPosts: signal<BlogPost[]>([]),
      selectedCategory: signal<string | null>(null),
      selectedMonth: signal<string | null>(null),
      sidebarOpen: signal<boolean>(true),
      loadProfileFromBackend: loadProfileSpy,
      setSelectedCategory: vi.fn(),
      setSelectedMonth: vi.fn(),
      setSearchQuery: vi.fn(),
      navigateTo: vi.fn(),
    };

    const mockPresenceService = {
      isAuthorOnline: signal<boolean>(true),
      presenceStatus: signal<'connected' | 'connecting' | 'disconnected'>('connected'),
    };

    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [
        { provide: BlogService, useValue: mockBlogService },
        { provide: PresenceService, useValue: mockPresenceService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
  });

  it('should call loadProfileFromBackend() on ngOnInit to fetch latest author avatar in guest mode', () => {
    fixture.detectChanges();
    expect(loadProfileSpy).toHaveBeenCalledTimes(1);
  });

  it('should render app-author-avatar with xl size and current profile avatar', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const authorAvatar = compiled.querySelector('app-author-avatar');

    expect(authorAvatar).toBeTruthy();
  });

  it('should update avatar reactively when profile signal receives new avatarUrl from server', () => {
    fixture.detectChanges();

    mockProfile.update((prev) => ({
      ...prev,
      avatar_url: 'https://res.cloudinary.com/demo/image/upload/v12345/new-avatar.webp',
    }));
    fixture.detectChanges();

    expect(component.profile().avatar_url).toBe(
      'https://res.cloudinary.com/demo/image/upload/v12345/new-avatar.webp'
    );
  });
});
