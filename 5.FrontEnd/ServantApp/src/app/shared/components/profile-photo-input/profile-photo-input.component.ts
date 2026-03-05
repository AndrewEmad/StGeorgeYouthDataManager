import {
  Component,
  input,
  output,
  viewChild,
  signal,
  computed,
  OnDestroy,
  effect,
  ElementRef,
} from '@angular/core';

@Component({
  selector: 'app-profile-photo-input',
  standalone: true,
  templateUrl: './profile-photo-input.component.html',
  styleUrl: './profile-photo-input.component.scss',
})
export class ProfilePhotoInputComponent implements OnDestroy {
  previewUrl = input<string | null>(null);
  maxSizeBytes = input<number>(5 * 1024 * 1024);
  label = input<string>('تغيير الصورة');
  disabled = input<boolean>(false);
  applySizeLimit = input<boolean>(true);

  photoSelected = output<File>();

  fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  localPreviewUrl = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  displayUrl = computed(() => this.localPreviewUrl() ?? this.previewUrl());

  private objectUrl: string | null = null;

  constructor() {
    effect(() => {
      if (this.previewUrl()) this.localPreviewUrl.set(null);
    });
  }

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
