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
  selector: 'servant-text-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldWrapperComponent],
  templateUrl: './text-input.component.html',
  styleUrl: './text-input.component.css',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: TextInputComponent, multi: true },
  ],
})
export class TextInputComponent implements ControlValueAccessor {
  private readonly uniqueId = `text-${Math.random().toString(36).slice(2, 10)}`;

  label = input.required<string>();
  controlId = input<string | null>(null);
  placeholder = input<string>('');
  type = input<string>('text');
  required = input<boolean>(false);
  error = input<string | null>(null);
  hint = input<string | null>(null);
  disabled = input<boolean>(false);

  readonly id = computed(() => this.controlId() ?? this.uniqueId);

  protected value = signal<string>('');
  protected onChange: (value: string) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    // Handled via input binding; parent can pass [disabled]="control.disabled"
  }

  protected onInput(e: Event): void {
    const v = (e.target as HTMLInputElement).value;
    this.value.set(v);
    this.onChange(v);
  }

  protected onBlur(): void {
    this.onTouched();
  }
}
