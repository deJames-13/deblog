import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SearchFilterBarComponent } from './search-filter-bar.component';

describe('SearchFilterBarComponent', () => {
  let component: SearchFilterBarComponent;
  let fixture: ComponentFixture<SearchFilterBarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchFilterBarComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchFilterBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the search filter bar', () => {
    expect(component).toBeTruthy();
  });

  it('should display total entries count', () => {
    fixture.componentRef.setInput('totalEntries', 18);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('ENTRIES: 18');
  });

  it('should emit searchChange on input', () => {
    let query = '';
    component.searchChange.subscribe((q) => (query = q));

    const input = fixture.nativeElement.querySelector('#landing-search-input') as HTMLInputElement;
    input.value = 'kernel';
    input.dispatchEvent(new Event('input'));

    expect(query).toBe('kernel');
  });
});
