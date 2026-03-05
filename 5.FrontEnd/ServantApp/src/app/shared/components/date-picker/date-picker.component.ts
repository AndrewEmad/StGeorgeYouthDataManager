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
  selector: 'servant-date-picker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldWrapperComponent],
  templateUrl: './date-picker.component.html',
  styleUrl: './date-picker.component.css',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: DatePickerComponent, multi: true },
  ],
})
export class DatePickerComponent implements ControlValueAccessor {
  private readonly uniqueId = `date-${Math.random().toString(36).slice(2, 10)}`;

  label = input.required<string>();
  controlId = input<string | null>(null);
  /** 'date' | 'datetime-local' */
  mode = input<'date' | 'datetime-local'>('date');
  required = input<boolean>(false);
  error = input<string | null>(null);
  hint = input<string | null>(null);
  disabled = input<boolean>(false);

  readonly id = computed(() => this.controlId() ?? this.uniqueId);

  /** Internal value: ISO string or empty string for binding to input. */
  protected value = signal<string>('');
  protected onChange: (value: string | null) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: string | Date | null | undefined): void {
    if (value == null || value === '') {
      this.value.set('');
      return;
    }
    const str = typeof value === 'string' ? value : value.toISOString();
    this.value.set(this.mode() === 'date' ? str.split('T')[0] ?? '' : str.slice(0, 16));
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(_isDisabled: boolean): void {}

  protected onInput(e: Event): void {
    const v = (e.target as HTMLInputElement).value;
    this.value.set(v);
    if (!v) {
      this.onChange(null);
      return;
    }
    this.onChange(this.mode() === 'date' ? v : new Date(v).toISOString());
  }

  protected onBlur(): void {
    this.onTouched();
  }

  protected triggerShowPicker(input: HTMLInputElement): void {
    if (typeof input?.showPicker === 'function') {
      input.showPicker();
    }
  }
}
