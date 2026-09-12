import {
  Component,
  ElementRef,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { BlogService } from '../../core/services/blog.service';

@Component({
  selector: 'app-markdown-renderer',
  template: `
    <div
      #contentContainer
      [class]="'markdown-content leading-relaxed text-[var(--text-primary)] ' + customClass()"
      [innerHTML]="renderedHtml()"
      (click)="handleContainerClick($event)"
    ></div>
  `,
  styles: `
    :host ::ng-deep .markdown-content h1 {
      font-size: 1.5rem;
      font-weight: 700;
      letter-spacing: -0.025em;
      color: var(--text-primary);
      margin-top: 1.5rem;
      margin-bottom: 0.75rem;
      padding-bottom: 0.25rem;
      border-bottom: 1px solid var(--border-subtle);
    }
    @media (min-width: 640px) {
      :host ::ng-deep .markdown-content h1 {
        font-size: 1.875rem;
      }
    }
    :host ::ng-deep .markdown-content h2 {
      font-size: 1.25rem;
      font-weight: 700;
      letter-spacing: -0.025em;
      color: var(--text-primary);
      margin-top: 1.25rem;
      margin-bottom: 0.5rem;
      padding-bottom: 0.25rem;
      border-bottom: 1px solid var(--border-subtle);
    }
    @media (min-width: 640px) {
      :host ::ng-deep .markdown-content h2 {
        font-size: 1.5rem;
      }
    }
    :host ::ng-deep .markdown-content h3 {
      font-size: 1.125rem;
      font-weight: 700;
      color: var(--text-primary);
      margin-top: 1rem;
      margin-bottom: 0.5rem;
    }
    :host ::ng-deep .markdown-content h4 {
      font-size: 1rem;
      font-weight: 700;
      color: var(--text-primary);
      margin-top: 0.75rem;
      margin-bottom: 0.25rem;
    }
    :host ::ng-deep .markdown-content p {
      margin-top: 0.75rem;
      margin-bottom: 0.75rem;
      line-height: 1.625;
      color: var(--text-primary);
    }
    :host ::ng-deep .markdown-content ul {
      margin: 0.75rem 0;
      padding-left: 1.25rem;
      list-style-type: disc;
    }
    :host ::ng-deep .markdown-content ul li::marker {
      color: var(--accent-terminal);
    }
    :host ::ng-deep .markdown-content ol {
      margin: 0.75rem 0;
      padding-left: 1.25rem;
      list-style-type: decimal;
      font-family: 'JetBrains Mono', 'Courier New', monospace;
      font-size: 0.8125rem;
    }
    :host ::ng-deep .markdown-content ol li::marker {
      color: var(--accent-terminal);
    }
    :host ::ng-deep .markdown-content li {
      line-height: 1.625;
      margin-bottom: 0.25rem;
    }
    :host ::ng-deep .markdown-content blockquote {
      margin: 1rem 0;
      padding: 0.375rem 0 0.375rem 1rem;
      border-left: 2px solid var(--accent-terminal);
      background-color: var(--bg-subtle);
      font-style: italic;
      color: var(--text-muted);
    }
    :host ::ng-deep .markdown-content hr {
      margin: 1.5rem 0;
      border: 0;
      border-top: 1px solid var(--border-subtle);
    }
    :host ::ng-deep .markdown-content a {
      color: var(--accent-terminal);
      font-weight: 600;
      text-decoration: underline;
    }
    :host ::ng-deep .markdown-content a:hover {
      opacity: 0.85;
    }
    :host ::ng-deep .markdown-content table {
      width: 100%;
      text-align: left;
      font-size: 0.75rem;
      font-family: 'JetBrains Mono', monospace;
      border-collapse: collapse;
      margin: 1rem 0;
      border: 1px solid var(--border-color);
    }
    :host ::ng-deep .markdown-content thead {
      background-color: var(--bg-surface);
      border-bottom: 1px solid var(--border-color);
      color: var(--text-primary);
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    :host ::ng-deep .markdown-content th,
    :host ::ng-deep .markdown-content td {
      padding: 0.5rem 0.75rem;
      border-right: 1px solid var(--border-subtle);
    }
    :host ::ng-deep .markdown-content th:last-child,
    :host ::ng-deep .markdown-content td:last-child {
      border-right: none;
    }
    :host ::ng-deep .markdown-content tbody tr {
      border-bottom: 1px solid var(--border-subtle);
      background-color: var(--bg-canvas);
    }
    :host ::ng-deep .markdown-content tbody tr:last-child {
      border-bottom: none;
    }
    :host ::ng-deep .markdown-content code:not(pre code) {
      padding: 0.125rem 0.375rem;
      margin: 0 0.125rem;
      background-color: var(--bg-canvas);
      border: 1px solid var(--border-subtle);
      font-family: 'JetBrains Mono', monospace;
      font-size: 0.75rem;
      color: var(--accent-terminal);
      border-radius: 0 !important;
    }
    :host ::ng-deep .bios-code-block {
      margin: 1rem 0;
      border: 1px solid var(--border-color);
      background-color: var(--bg-canvas);
      font-family: 'JetBrains Mono', monospace;
      font-size: 0.75rem;
      color: var(--text-primary);
      overflow: hidden;
    }
    :host ::ng-deep .bios-code-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.375rem 0.75rem;
      background-color: var(--bg-surface);
      border-bottom: 1px solid var(--border-subtle);
      font-size: 0.625rem;
      color: var(--text-dim);
      text-transform: uppercase;
      user-select: none;
    }
    :host ::ng-deep .bios-code-copy-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      background: transparent;
      border: none;
      color: var(--text-dim);
      cursor: pointer;
      font-family: 'JetBrains Mono', monospace;
      font-size: 0.625rem;
      transition: color 150ms ease;
    }
    :host ::ng-deep .bios-code-copy-btn:hover {
      color: var(--text-primary);
    }
    :host ::ng-deep .bios-code-pre {
      padding: 0.875rem;
      margin: 0;
      overflow-x: auto;
      line-height: 1.625;
      background-color: var(--bg-canvas);
    }
    :host ::ng-deep .bios-code-pre code {
      font-family: 'JetBrains Mono', monospace;
      font-size: 0.75rem;
      color: var(--text-primary);
      background: transparent;
      border: none;
      padding: 0;
    }
  `,
})
export class MarkdownRendererComponent {
  private readonly blogService = inject(BlogService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly content = input<string>('');
  readonly customClass = input<string>('');
  readonly contentContainer = viewChild<ElementRef<HTMLElement>>('contentContainer');

  readonly copiedCodeId = signal<string | null>(null);

  readonly renderedHtml = computed<SafeHtml>(() => {
    const raw = this.content();
    if (!raw) return '';

    // Configure marked renderer for BIOS code blocks
    const renderer = new marked.Renderer();
    renderer.code = ({ text, lang }) => {
      const language = lang || 'code';
      const encodedCode = encodeURIComponent(text);
      return `
        <div class="bios-code-block">
          <div class="bios-code-header">
            <span class="font-semibold tracking-wider">SOURCE [${language}]</span>
            <button
              type="button"
              class="bios-code-copy-btn"
              data-copy-code="${encodedCode}"
              title="Copy code snippet"
            >
              <span>[COPY]</span>
            </button>
          </div>
          <pre class="bios-code-pre"><code>${this.escapeHtml(text)}</code></pre>
        </div>
      `;
    };

    const parsed = marked.parse(raw, {
      renderer,
      gfm: true,
      breaks: true,
    });

    return this.sanitizer.bypassSecurityTrustHtml(parsed as string);
  });

  handleContainerClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const copyBtn = target.closest('[data-copy-code]') as HTMLElement | null;
    if (copyBtn) {
      const code = decodeURIComponent(copyBtn.getAttribute('data-copy-code') || '');
      if (code) {
        navigator.clipboard.writeText(code).then(
          () => {
            copyBtn.innerHTML =
              '<span style="color: var(--accent-terminal); font-weight: 700;">[COPIED]</span>';
            this.blogService.addToast('Code copied to clipboard', 'info');
            setTimeout(() => {
              copyBtn.innerHTML = '<span>[COPY]</span>';
            }, 2000);
          },
          () => {
            this.blogService.addToast('Failed to copy code', 'error');
          }
        );
      }
    }
  }

  private escapeHtml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }
}
