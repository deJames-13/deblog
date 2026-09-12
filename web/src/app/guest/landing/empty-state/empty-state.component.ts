import { Component, input, output } from '@angular/core';
import { LucideBookOpen } from '@lucide/angular';

@Component({
  selector: 'app-empty-state',
  imports: [LucideBookOpen],
  templateUrl: './empty-state.component.html',
})
export class EmptyStateComponent {
  readonly hasActiveFilters = input<boolean>(false);
  readonly clearAllFilters = output<void>();
}
