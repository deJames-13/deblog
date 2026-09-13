import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TooltipDirective } from './tooltip.directive';

@Component({
  imports: [TooltipDirective],
  template: `
    <button id="test-btn" [appTooltip]="tooltipText">Action</button>
  `,
})
class TestHostComponent {
  tooltipText = 'Edit Entry';
}

describe('TooltipDirective', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();
  });

  afterEach(() => {
    // Clean up any remaining tooltips from document.body
    document.querySelectorAll('.deblog-bios-tooltip').forEach((el) => el.remove());
  });

  it('should set aria-label on host element', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector('#test-btn');
    expect(btn?.getAttribute('aria-label')).toBe('Edit Entry');
  });

  it('should create and display tooltip on mouseenter and remove on mouseleave', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector('#test-btn') as HTMLButtonElement;

    btn.dispatchEvent(new MouseEvent('mouseenter'));
    fixture.detectChanges();

    const tip = document.querySelector('.deblog-bios-tooltip');
    expect(tip).toBeTruthy();
    expect(tip?.textContent).toBe('Edit Entry');

    btn.dispatchEvent(new MouseEvent('mouseleave'));
    fixture.detectChanges();

    const tipAfter = document.querySelector('.deblog-bios-tooltip');
    expect(tipAfter).toBeNull();
  });
});
