import { Component, computed, input, output } from '@angular/core';
import { LucideFilter, LucideSearch, LucideX } from '@lucide/angular';

@Component({
  selector: 'app-search-filter-bar',
  imports: [LucideSearch, LucideX, LucideFilter],
  templateUrl: './search-filter-bar.component.html',
})
export class SearchFilterBarComponent {
  readonly searchQuery = input<string>('');
  readonly selectedCategory = input<string | null>(null);
  readonly selectedMonth = input<string | null>(null);
  readonly totalEntries = input<number>(0);

  readonly searchChange = output<string>();
  readonly clearSearch = output<void>();
  readonly clearCategory = output<void>();
  readonly clearMonth = output<void>();
  readonly clearAllFilters = output<void>();

  readonly hasActiveFilters = computed(() => {
    return !!this.selectedCategory() || !!this.selectedMonth() || !!this.searchQuery().trim();
  });
}
