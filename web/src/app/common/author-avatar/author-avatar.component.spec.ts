import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuthorAvatarComponent } from './author-avatar.component';

describe('AuthorAvatarComponent', () => {
  let component: AuthorAvatarComponent;
  let fixture: ComponentFixture<AuthorAvatarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuthorAvatarComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(AuthorAvatarComponent);
    component = fixture.componentInstance;
  });

  it('should create and render fallback initial when no avatar URL is provided', () => {
    fixture.componentRef.setInput('name', 'Derick Espinosa');
    fixture.componentRef.setInput('avatarUrl', undefined);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const fallback = compiled.querySelector('[title="Derick Espinosa"]');
    expect(fallback).toBeTruthy();
    expect(fallback?.textContent?.trim()).toBe('D');
  });

  it('should normalize public/ assets path into root-relative path', () => {
    fixture.componentRef.setInput('avatarUrl', 'public/assets/images/me.png');
    fixture.detectChanges();

    expect(component.resolvedAvatarUrl()).toBe('/assets/images/me.png');
  });

  it('should support xl size mapping to w-16 h-16 for guest profile sidebar', () => {
    fixture.componentRef.setInput('size', 'xl');
    fixture.detectChanges();

    expect(component.imgClass()).toContain('w-16 h-16');
    expect(component.fallbackClass()).toContain('w-16 h-16');
  });

  it('should reset imageError signal when avatarUrl input updates with a new URL', () => {
    fixture.componentRef.setInput('avatarUrl', 'https://example.com/broken.png');
    fixture.detectChanges();

    // Trigger image error
    component.imageError.set(true);
    fixture.detectChanges();
    expect(component.imageError()).toBe(true);

    // Provide new avatar URL (e.g. from Cloudinary fetch)
    fixture.componentRef.setInput('avatarUrl', 'https://res.cloudinary.com/demo/image/upload/v1/avatar.webp');
    fixture.detectChanges();

    expect(component.imageError()).toBe(false);
  });

  it('should render accessible status dot when showStatus is true', () => {
    fixture.componentRef.setInput('showStatus', true);
    fixture.componentRef.setInput('isOnline', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const statusDot = compiled.querySelector('[role="status"]');
    expect(statusDot).toBeTruthy();
    expect(statusDot?.getAttribute('aria-label')).toBe('Author is online');
  });
});
