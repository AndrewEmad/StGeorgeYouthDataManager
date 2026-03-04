import {
  Component,
  input,
  output,
  signal,
  ChangeDetectionStrategy,
  ElementRef,
  viewChild,
} from '@angular/core';

/**
 * Emits the selected File. Parent is responsible for uploading and storing the path.
 * Optionally display a current image URL (e.g. after upload or existing profile photo).
 */
@Component({
  selector: 'admin-image-upload',
  standalone: true,
  templateUrl: './image-upload.component.html',
  styleUrl: './image-upload.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImageUploadComponent {
  label = input<string>('');
  currentImageUrl = input<string | null>(null);
  accept = input<string>('image/*');
  disabled = input<boolean>(false);

  fileSelected = output<File>();

  previewUrl = signal<string | null>(null);
  fileInputRef = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  onFileChange(e: Event): void {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) return;
    this.clearPreview();
    const url = URL.createObjectURL(file);
    this.previewUrl.set(url);
    this.fileSelected.emit(file);
    input.value = '';
  }

  clearPreview(): void {
    const url = this.previewUrl();
    if (url) URL.revokeObjectURL(url);
    this.previewUrl.set(null);
  }

  triggerSelect(): void {
    if (this.disabled()) return;
    this.fileInputRef()?.nativeElement?.click();
  }
}
