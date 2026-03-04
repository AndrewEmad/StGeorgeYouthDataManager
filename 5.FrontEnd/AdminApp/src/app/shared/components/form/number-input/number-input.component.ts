import {
  Component,
  input,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'admin-number-input',
  standalone: true,
  templateUrl: './number-input.component.html',
  styleUrl: './number-input.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: NumberInputComponent,
      multi: true,
    },
  ],
})
export class NumberInputComponent implements ControlValueAccessor {
  label = input<string>('');
  placeholder = input<string>('');
  id = input<string>('');
  disabled = input<boolean>(false);
  required = input<boolean>(false);
  min = input<number | null>(null);
  max = input<number | null>(null);
  step = input<number | null>(null);

  value = signal<number | null>(null);
  isDisabled = signal(false);

  private onChange: (value: number | null) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: number | null): void {
    this.value.set(value ?? null);
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
  }

  onInput(e: Event): void {
    const raw = (e.target as HTMLInputElement).value;
    const v = raw === '' ? null : Number(raw);
    if (v !== null && !Number.isNaN(v)) {
      this.value.set(v);
      this.onChange(v);
    } else {
      this.value.set(null);
      this.onChange(null);
    }
  }

  onBlur(): void {
    this.onTouched();
  }
}
