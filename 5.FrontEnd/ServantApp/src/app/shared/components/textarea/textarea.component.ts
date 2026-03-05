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
  selector: 'servant-textarea',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldWrapperComponent],
  templateUrl: './textarea.component.html',
  styleUrl: './textarea.component.css',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: TextareaComponent, multi: true },
  ],
})
export class TextareaComponent implements ControlValueAccessor {
  private readonly uniqueId = `ta-${Math.random().toString(36).slice(2, 10)}`;

  label = input.required<string>();
  controlId = input<string | null>(null);
  placeholder = input<string>('');
  required = input<boolean>(false);
  rows = input<number>(3);
  maxLength = input<number | null>(null);
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

  setDisabledState(_isDisabled: boolean): void {}

  protected onInput(e: Event): void {
    const v = (e.target as HTMLTextAreaElement).value;
    this.value.set(v);
    this.onChange(v);
  }

  protected onBlur(): void {
    this.onTouched();
  }
}
