import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PostCardComponent } from './post-card.component';
import { BlogPost } from '../../../core/models/blog.model';

const mockPost: BlogPost = {
  id: 'test-1',
  slug: 'test-slug',
  title: 'Test Engineering Article',
  subtitle: 'A guide to system design',
  excerpt: 'This is a test excerpt that explains the architecture.',
  content: '# Hello World',
  category: 'Architecture',
  tags: ['systems', 'design'],
  cover_image: 'https://images.unsplash.com/photo-1518770660439-4636190af475',
  created_at: '2026-03-10T10:00:00Z',
  updated_at: '2026-03-10T10:00:00Z',
  views_count: 142,
  likes_count: 38,
  reading_time_minutes: 5,
  status: 'published',
};

describe('PostCardComponent', () => {
  let component: PostCardComponent;
  let fixture: ComponentFixture<PostCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PostCardComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('post', mockPost);
    fixture.detectChanges();
  });

  it('should render the post title and category', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.card-title')?.textContent).toContain('Test Engineering Article');
    expect(compiled.querySelector('.card-category-badge')?.textContent).toContain('Architecture');
  });

  it('should render the cover banner image wrapper when cover_image exists', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.card-banner-wrapper')).toBeTruthy();
    expect(compiled.querySelector('.card-banner-img')).toBeTruthy();
  });

  it('should emit view event when clicked', () => {
    let emittedPost: BlogPost | undefined;
    component.view.subscribe((p) => (emittedPost = p));

    const card = fixture.nativeElement.querySelector('.guest-post-card') as HTMLElement;
    card.click();

    expect(emittedPost).toEqual(mockPost);
  });
});
