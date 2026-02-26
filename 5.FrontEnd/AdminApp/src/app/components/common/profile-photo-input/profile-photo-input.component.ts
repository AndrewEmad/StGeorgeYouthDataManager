import { Component, input, output, viewChild, signal, computed, OnDestroy, effect, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-profile-photo-input',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-photo-input.component.html',
  styleUrl: './profile-photo-input.component.scss',
})
export class ProfilePhotoInputComponent implements OnDestroy {
  /** Current preview URL (from parent: existing photo or after crop). */
  previewUrl = input<string | null>(null);
  /** Max file size in bytes. Default 5MB. */
  maxSizeBytes = input<number>(5 * 1024 * 1024);
  /** Accessibility label for the trigger. */
  label = input<string>('تغيير الصورة');
  /** When true, clicking does nothing. */
  disabled = input<boolean>(false);
  /** When false, size limit is not checked here (e.g. when parent applies limit after crop). Default true. */
  applySizeLimit = input<boolean>(true);

  photoSelected = output<File>();

  fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  localPreviewUrl = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  /** Effective preview: local selection takes precedence over parent URL. */
  displayUrl = computed(() => this.localPreviewUrl() ?? this.previewUrl());

  constructor() {
    effect(() => {
      if (this.previewUrl()) this.localPreviewUrl.set(null);
    });
  }

  private objectUrl: string | null = null;

  ngOnDestroy() {
    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
  }

  triggerFileSelect() {
    if (this.disabled()) return;
    this.errorMessage.set(null);
    this.fileInput()?.nativeElement?.click();
  }

  onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input?.files?.[0];
    input.value = '';
    this.errorMessage.set(null);

    if (!file) return;

    const type = (file.type || '').toLowerCase();
    if (!type.startsWith('image/')) {
      this.errorMessage.set('يرجى اختيار ملف صورة (مثل JPEG أو PNG)');
      return;
    }

    if (this.applySizeLimit()) {
      const max = this.maxSizeBytes();
      if (file.size > max) {
        const mb = (max / (1024 * 1024)).toFixed(1);
        this.errorMessage.set(`الحد الأقصى لحجم الملف ${mb} ميجابايت`);
        return;
      }
    }

    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
    this.objectUrl = URL.createObjectURL(file);
    this.localPreviewUrl.set(this.objectUrl);
    this.photoSelected.emit(file);
  }
}
