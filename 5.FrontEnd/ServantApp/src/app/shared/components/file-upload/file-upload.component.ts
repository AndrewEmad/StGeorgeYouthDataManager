import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FormFieldWrapperComponent } from '../form-field-wrapper/form-field-wrapper.component';

@Component({
  selector: 'servant-file-upload',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldWrapperComponent],
  templateUrl: './file-upload.component.html',
  styleUrl: './file-upload.component.css',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: FileUploadComponent, multi: true },
  ],
})
export class FileUploadComponent implements ControlValueAccessor {
  private readonly uniqueId = `file-${Math.random().toString(36).slice(2, 10)}`;

  label = input.required<string>();
  controlId = input<string | null>(null);
  accept = input<string>('');
  required = input<boolean>(false);
  error = input<string | null>(null);
  hint = input<string | null>(null);
  disabled = input<boolean>(false);
  /** Allow multiple files. */
  multiple = input<boolean>(false);

  readonly id = computed(() => this.controlId() ?? this.uniqueId);

  /** Value is File | File[] | null depending on multiple(). */
  protected value = signal<File | File[] | null>(null);
  protected onChange: (value: File | File[] | null) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(_value: File | File[] | null): void {
    // File inputs cannot be set programmatically for security; we only reflect internal state
    this.value.set(_value ?? null);
  }

  registerOnChange(fn: (value: File | File[] | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(_isDisabled: boolean): void {}

  protected onInput(e: Event): void {
    const input = e.target as HTMLInputElement;
    const files = input.files;
    if (!files?.length) {
      this.value.set(null);
      this.onChange(null);
      return;
    }
    const v = this.multiple() ? Array.from(files) : files[0];
    this.value.set(v);
    this.onChange(v);
  }

  protected onBlur(): void {
    this.onTouched();
  }

  /** File names for display when value is set. */
  protected fileNames = computed(() => {
    const v = this.value();
    if (!v) return '';
    if (Array.isArray(v)) return v.map((f) => f.name).join(', ');
    return v.name;
  });
}
