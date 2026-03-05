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
  selector: 'servant-number-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldWrapperComponent],
  templateUrl: './number-input.component.html',
  styleUrl: './number-input.component.css',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: NumberInputComponent, multi: true },
  ],
})
export class NumberInputComponent implements ControlValueAccessor {
  private readonly uniqueId = `num-${Math.random().toString(36).slice(2, 10)}`;

  label = input.required<string>();
  controlId = input<string | null>(null);
  placeholder = input<string>('');
  required = input<boolean>(false);
  error = input<string | null>(null);
  hint = input<string | null>(null);
  disabled = input<boolean>(false);
  min = input<number | null>(null);
  max = input<number | null>(null);
  step = input<number | string>('1');

  readonly id = computed(() => this.controlId() ?? this.uniqueId);

  protected value = signal<number | null>(null);
  protected onChange: (value: number | null) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: number | null | undefined): void {
    this.value.set(value ?? null);
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(_isDisabled: boolean): void {}

  protected onInput(e: Event): void {
    const raw = (e.target as HTMLInputElement).value;
    const v = raw === '' ? null : Number(raw);
    this.value.set(v);
    this.onChange(v);
  }

  protected onBlur(): void {
    this.onTouched();
  }
}
