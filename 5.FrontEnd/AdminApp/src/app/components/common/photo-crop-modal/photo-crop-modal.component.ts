import { Component, input, output, viewChild, signal, computed } from '@angular/core';
import { ImageCropperComponent, ImageCroppedEvent } from 'ngx-image-cropper';

@Component({
  selector: 'app-photo-crop-modal',
  standalone: true,
  imports: [ImageCropperComponent],
  templateUrl: './photo-crop-modal.component.html',
  styleUrl: './photo-crop-modal.component.css',
})
export class PhotoCropModalComponent {
  sourceFile = input<File | null>(null);
  visible = computed(() => !!this.sourceFile());

  cropperRef = viewChild<ImageCropperComponent>(ImageCropperComponent);
  cropError = signal<string | null>(null);
  cropping = signal(false);

  croppedFile = output<File>();
  cancel = output<void>();

  onConfirm(): void {
    const cropper = this.cropperRef();
    if (!cropper) {
      this.cropError.set('لم يتم تحميل الصورة بعد');
      return;
    }
    this.cropping.set(true);
    this.cropError.set(null);
    const result = cropper.crop('blob');
    if (result == null) {
      this.cropping.set(false);
      this.cropError.set('تعذر قص الصورة');
      return;
    }
    const MAX_SIZE_BYTES = 5 * 1024 * 1024; // 5MB limit after crop
    (result as Promise<ImageCroppedEvent>).then((ev) => {
      this.cropping.set(false);
      if (ev?.blob) {
        const isPng = (ev.blob.type || '').toLowerCase().includes('png');
        const name = isPng ? 'photo.png' : 'photo.jpg';
        const type = ev.blob.type || 'image/png';
        const file = new File([ev.blob], name, { type });
        if (file.size > MAX_SIZE_BYTES) {
          this.cropError.set('حجم الصورة بعد القص يتجاوز 5 ميجابايت. جرّب تقليل جودة أو المساحة المقتطعة.');
          return;
        }
        this.croppedFile.emit(file);
      } else {
        this.cropError.set('تعذر الحصول على الصورة');
      }
    }).catch(() => {
      this.cropping.set(false);
      this.cropError.set('تعذر قص الصورة');
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }

  onLoadFailed(): void {
    this.cropError.set('فشل تحميل الصورة');
  }
}
