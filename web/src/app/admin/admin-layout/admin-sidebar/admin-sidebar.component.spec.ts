import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AdminSidebarComponent } from './admin-sidebar.component';
import { BlogService } from '../../../core/services/blog.service';

describe('AdminSidebarComponent', () => {
  let component: AdminSidebarComponent;
  let fixture: ComponentFixture<AdminSidebarComponent>;
  let blogService: BlogService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminSidebarComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminSidebarComponent);
    component = fixture.componentInstance;
    blogService = TestBed.inject(BlogService);
    fixture.detectChanges();
  });

  it('should create the admin sidebar component', () => {
    expect(component).toBeTruthy();
  });

  it('should render all 6 admin navigation items', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = compiled.querySelectorAll('nav[aria-label="Admin Sections"] button');
    expect(buttons.length).toBe(6);
  });

  it('should switch adminTab on button click', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const postsButton = compiled.querySelector('#admin-sidebar-nav-posts') as HTMLButtonElement;
    expect(postsButton).toBeTruthy();

    postsButton.click();
    fixture.detectChanges();

    expect(blogService.adminTab()).toBe('posts');
  });
});
