import { Component, inject } from '@angular/core';
import {
  LucideCpu,
  LucidePalette,
  LucideShield,
  LucideTerminal,
  LucideType,
  LucideX,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';

@Component({
  selector: 'app-help-modal',
  host: {
    '(window:keydown.escape)': 'handleEscape()',
  },
  imports: [
    LucideX,
    LucideTerminal,
    LucideCpu,
    LucidePalette,
    LucideType,
    LucideShield,
  ],
  templateUrl: './help-modal.component.html',
})
export class HelpModalComponent {
  private readonly blogService = inject(BlogService);

  readonly isOpen = this.blogService.helpModalOpen;

  readonly shortcuts = [
    { key: 'F1', desc: 'Display this deblog System Guide & Help Overlay' },
    { key: 'F2', desc: 'Focus Search Bar or search through articles and tags' },
    { key: 'F3', desc: 'Toggle Zen Reading Mode (centered fixed reading length)' },
    { key: 'F4', desc: 'Cycle Theme (Night, Twilight, Sepia, Soft Light)' },
    { key: 'F5', desc: 'Return to Landing Index and clear search filters' },
    { key: 'F6', desc: 'Cycle Alternate Reading Font (Roboto, Alice, Noto, etc.)' },
    { key: 'F7', desc: 'Filter Top Articles by reader likes and engagement' },
    { key: 'F8', desc: 'Switch to Admin Management Console (Authentication Gate)' },
    { key: 'ESC', desc: 'Exit Zen Mode, close modals, or return to index' },
  ];

  handleEscape(): void {
    if (this.isOpen()) {
      this.close();
    }
  }

  close(): void {
    this.blogService.closeHelpModal();
  }
}
