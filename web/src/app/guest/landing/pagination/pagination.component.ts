import { Component, input, output } from '@angular/core';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';

@Component({
  selector: 'app-pagination',
  imports: [LucideChevronLeft, LucideChevronRight],
  templateUrl: './pagination.component.html',
})
export class PaginationComponent {
  readonly currentPage = input<number>(1);
  readonly totalPages = input<number>(1);
  readonly pageNumbers = input<number[]>([]);

  readonly pageChange = output<number>();
}
