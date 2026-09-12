import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PaginationComponent } from './pagination.component';

describe('PaginationComponent', () => {
  let component: PaginationComponent;
  let fixture: ComponentFixture<PaginationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaginationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PaginationComponent);
    component = fixture.componentInstance;
  });

  it('should not render navigation if totalPages is 1', () => {
    fixture.componentRef.setInput('totalPages', 1);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#landing-pagination')).toBeNull();
  });

  it('should render page buttons when totalPages > 1', () => {
    fixture.componentRef.setInput('totalPages', 3);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('pageNumbers', [1, 2, 3]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = compiled.querySelectorAll('button');
    expect(buttons.length).toBe(5); // Prev + 3 pages + Next
  });

  it('should emit pageChange when a page number is clicked', () => {
    fixture.componentRef.setInput('totalPages', 3);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('pageNumbers', [1, 2, 3]);
    fixture.detectChanges();

    let targetPage = 0;
    component.pageChange.subscribe((p) => (targetPage = p));

    const page2Button = fixture.nativeElement.querySelectorAll('button')[2]; // 3rd button is page 2
    page2Button.click();

    expect(targetPage).toBe(2);
  });
});
