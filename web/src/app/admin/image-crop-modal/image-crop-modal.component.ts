import {
  Component,
  ElementRef,
  computed,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideCheck,
  LucideMove,
  LucideRefreshCw,
  LucideRotateCw,
  LucideX,
  LucideZoomIn,
  LucideZoomOut,
} from '@lucide/angular';

@Component({
  selector: 'app-image-crop-modal',
  imports: [
    FormsModule,
    LucideMove,
    LucideX,
    LucideRefreshCw,
    LucideZoomIn,
    LucideZoomOut,
    LucideRotateCw,
    LucideCheck,
  ],
  host: {
    '(window:mousemove)': 'handleWindowMouseMove($event)',
    '(window:mouseup)': 'handleWindowMouseUp($event)',
    '(window:keydown)': 'handleWindowKeyDown($event)',
  },
  templateUrl: './image-crop-modal.component.html',
})
export class ImageCropModalComponent {
  protected readonly Math = Math;

  readonly isOpen = input<boolean>(false);
  readonly imageSrc = input<string>('');
  readonly aspectRatio = input<number>(1);
  readonly modalTitle = input<string>('Crop Image Asset');

  readonly cropComplete = output<string>();
  readonly closeModal = output<void>();

  readonly containerWidth = 460;
  readonly containerHeight = 340;

  readonly zoom = signal<number>(1);
  readonly rotation = signal<number>(0);
  readonly offset = signal<{ x: number; y: number }>({ x: 0, y: 0 });
  readonly isDragging = signal<boolean>(false);
  readonly imageLoaded = signal<boolean>(false);
  readonly naturalSize = signal<{ width: number; height: number }>({ width: 0, height: 0 });

  private dragStart = { x: 0, y: 0 };
  private offsetStart = { x: 0, y: 0 };
  private imageElement: HTMLImageElement | null = null;

  readonly cropBoxDimensions = computed(() => {
    const ratio = this.aspectRatio() || 1;
    let width = this.containerWidth - 40;
    let height = width / ratio;
    if (height > this.containerHeight - 40) {
      height = this.containerHeight - 40;
      width = height * ratio;
    }
    return { width, height };
  });

  constructor() {
    effect(() => {
      const open = this.isOpen();
      const src = this.imageSrc();
      if (open && src) {
        this.zoom.set(1);
        this.rotation.set(0);
        this.offset.set({ x: 0, y: 0 });
        this.imageLoaded.set(false);

        const img = new Image();
        img.crossOrigin = 'anonymous';
        img.onload = () => {
          this.imageElement = img;
          this.naturalSize.set({ width: img.naturalWidth, height: img.naturalHeight });
          this.imageLoaded.set(true);
        };
        img.src = src;
      }
    });
  }

  readonly renderedDimensions = computed(() => {
    const { width: cropW, height: cropH } = this.cropBoxDimensions();
    const nat = this.naturalSize();
    const rot = this.rotation();
    const isRotated = rot === 90 || rot === 270;
    const effW = isRotated ? nat.height : nat.width;
    const effH = isRotated ? nat.width : nat.height;

    const baseScale = nat.width > 0 ? Math.max(cropW / effW, cropH / effH) : 1;
    const z = this.zoom();
    return {
      width: nat.width * baseScale * z,
      height: nat.height * baseScale * z,
      baseScale,
    };
  });

  handleMouseDown(e: MouseEvent): void {
    e.preventDefault();
    this.isDragging.set(true);
    this.dragStart = { x: e.clientX, y: e.clientY };
    this.offsetStart = { ...this.offset() };
  }

  handleWindowMouseMove(e: MouseEvent): void {
    if (!this.isDragging()) return;
    const dx = e.clientX - this.dragStart.x;
    const dy = e.clientY - this.dragStart.y;
    this.offset.set({
      x: this.offsetStart.x + dx,
      y: this.offsetStart.y + dy,
    });
  }

  handleWindowMouseUp(e: MouseEvent): void {
    this.isDragging.set(false);
  }

  handleWindowKeyDown(e: KeyboardEvent): void {
    if (this.isOpen() && e.key === 'Escape') {
      this.closeModal.emit();
    }
  }

  handleWheel(e: WheelEvent): void {
    e.preventDefault();
    const delta = e.deltaY * -0.0015;
    this.zoom.update((prev) => Math.min(3, Math.max(0.5, prev + delta)));
  }

  rotate(): void {
    this.rotation.update((prev) => (prev + 90) % 360);
  }

  reset(): void {
    this.offset.set({ x: 0, y: 0 });
    this.zoom.set(1);
    this.rotation.set(0);
  }

  zoomIn(): void {
    this.zoom.update((prev) => Math.min(3, prev + 0.2));
  }

  zoomOut(): void {
    this.zoom.update((prev) => Math.max(0.6, prev - 0.2));
  }

  applyCrop(): void {
    if (!this.imageElement || !this.imageLoaded()) return;

    const ratio = this.aspectRatio() || 1;
    const targetWidth = ratio === 1 ? 500 : 1200;
    const targetHeight = targetWidth / ratio;
    const { width: cropW, height: cropH } = this.cropBoxDimensions();
    const nat = this.naturalSize();
    const rot = this.rotation();

    const canvas = document.createElement('canvas');
    canvas.width = targetWidth;
    canvas.height = targetHeight;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.fillStyle = '#18181b';
    ctx.fillRect(0, 0, targetWidth, targetHeight);

    const isRotated = rot === 90 || rot === 270;
    const effW = isRotated ? nat.height : nat.width;
    const effH = isRotated ? nat.width : nat.height;
    const baseScale = Math.max(cropW / effW, cropH / effH);
    const totalPreviewScale = baseScale * this.zoom();
    const previewToCanvasRatio = targetWidth / cropW;

    ctx.save();
    ctx.translate(targetWidth / 2, targetHeight / 2);
    ctx.translate(
      this.offset().x * previewToCanvasRatio,
      this.offset().y * previewToCanvasRatio
    );
    ctx.rotate((rot * Math.PI) / 180);

    const drawW = nat.width * totalPreviewScale * previewToCanvasRatio;
    const drawH = nat.height * totalPreviewScale * previewToCanvasRatio;
    ctx.drawImage(this.imageElement, -drawW / 2, -drawH / 2, drawW, drawH);
    ctx.restore();

    try {
      const dataUrl = canvas.toDataURL('image/webp', 0.92);
      this.cropComplete.emit(dataUrl);
      this.closeModal.emit();
    } catch {
      const fallback = canvas.toDataURL('image/jpeg', 0.9);
      this.cropComplete.emit(fallback);
      this.closeModal.emit();
    }
  }
}
