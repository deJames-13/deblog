import {
  Directive,
  ElementRef,
  OnDestroy,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';

export type TooltipPosition = 'top' | 'bottom' | 'left' | 'right';

@Directive({
  selector: '[appTooltip]',
  host: {
    '(mouseenter)': 'show()',
    '(mouseleave)': 'hide()',
    '(focusin)': 'show()',
    '(focusout)': 'hide()',
    '(keydown.escape)': 'hide()',
    '[attr.aria-label]': 'ariaLabel()',
  },
})
export class TooltipDirective implements OnDestroy {
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  readonly appTooltip = input<string>('');
  readonly tooltipPosition = input<TooltipPosition>('top');

  private tooltipEl: HTMLElement | null = null;
  private isVisible = false;

  readonly ariaLabel = computed(() => {
    const text = this.appTooltip();
    const existing = this.elementRef.nativeElement.getAttribute('aria-label');
    return existing || text || null;
  });

  show(): void {
    const text = this.appTooltip();
    if (!text || typeof document === 'undefined') return;

    this.hide();

    const tip = document.createElement('div');
    tip.className =
      'deblog-bios-tooltip fixed z-[9999] pointer-events-none font-mono text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 border border-[var(--accent-terminal)] bg-[var(--bg-canvas)] text-[var(--accent-terminal)] shadow-lg transition-opacity duration-100 whitespace-nowrap opacity-0';
    tip.textContent = text;
    tip.setAttribute('role', 'tooltip');
    tip.setAttribute('aria-hidden', 'true');

    document.body.appendChild(tip);
    this.tooltipEl = tip;
    this.isVisible = true;

    this.updatePosition();
    requestAnimationFrame(() => {
      if (this.tooltipEl && this.isVisible) {
        this.tooltipEl.style.opacity = '1';
      }
    });
  }

  hide(): void {
    this.isVisible = false;
    if (this.tooltipEl) {
      this.tooltipEl.remove();
      this.tooltipEl = null;
    }
  }

  private updatePosition(): void {
    if (!this.tooltipEl) return;

    const hostRect = this.elementRef.nativeElement.getBoundingClientRect();
    const tipRect = this.tooltipEl.getBoundingClientRect();
    const pos = this.tooltipPosition();

    let top = 0;
    let left = 0;
    const gap = 6;

    switch (pos) {
      case 'bottom':
        top = hostRect.bottom + gap;
        left = hostRect.left + (hostRect.width - tipRect.width) / 2;
        break;
      case 'left':
        top = hostRect.top + (hostRect.height - tipRect.height) / 2;
        left = hostRect.left - tipRect.width - gap;
        break;
      case 'right':
        top = hostRect.top + (hostRect.height - tipRect.height) / 2;
        left = hostRect.right + gap;
        break;
      case 'top':
      default:
        top = hostRect.top - tipRect.height - gap;
        left = hostRect.left + (hostRect.width - tipRect.width) / 2;
        // Flip to bottom if clipping screen top
        if (top < 4) {
          top = hostRect.bottom + gap;
        }
        break;
    }

    // Keep within viewport horizontally
    const viewportWidth = window.innerWidth;
    if (left < 6) left = 6;
    if (left + tipRect.width > viewportWidth - 6) {
      left = viewportWidth - tipRect.width - 6;
    }

    this.tooltipEl.style.top = `${Math.round(top)}px`;
    this.tooltipEl.style.left = `${Math.round(left)}px`;
  }

  ngOnDestroy(): void {
    this.hide();
  }
}
