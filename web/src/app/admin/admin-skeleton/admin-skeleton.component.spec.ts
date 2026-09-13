import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import { AdminSkeletonComponent } from './admin-skeleton.component';

describe('AdminSkeletonComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminSkeletonComponent],
    }).compileComponents();
  });

  it('should create the admin skeleton component', () => {
    const fixture = TestBed.createComponent(AdminSkeletonComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should render role=status and aria-busy=true for accessibility', () => {
    const fixture = TestBed.createComponent(AdminSkeletonComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const skeletonEl = compiled.querySelector('#admin-skeleton-view');
    expect(skeletonEl).toBeTruthy();
    expect(skeletonEl?.getAttribute('role')).toBe('status');
    expect(skeletonEl?.getAttribute('aria-busy')).toBe('true');
  });
});
