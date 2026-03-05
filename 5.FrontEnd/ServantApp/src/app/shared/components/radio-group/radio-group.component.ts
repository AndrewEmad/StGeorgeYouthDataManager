import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FormFieldWrapperComponent } from '../form-field-wrapper/form-field-wrapper.component';
import type { SelectOption } from '../../models/common.model';

@Component({
  selector: 'servant-radio-group',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldWrapperComponent],
  templateUrl: './radio-group.component.html',
  styleUrl: './radio-group.component.css',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: RadioGroupComponent, multi: true },
  ],
})
export class RadioGroupComponent implements ControlValueAccessor {
  private readonly uniqueId = `radio-${Math.random().toString(36).slice(2, 10)}`;

  label = input.required<string>();
  controlId = input<string | null>(null);
  options = input.required<SelectOption<string | number>[]>();
  required = input<boolean>(false);
  error = input<string | null>(null);
  hint = input<string | null>(null);
  disabled = input<boolean>(false);
  /** 'horizontal' | 'vertical' */
  layout = input<'horizontal' | 'vertical'>('vertical');

  readonly id = computed(() => this.controlId() ?? this.uniqueId);

  protected value = signal<string | number | null>(null);
  protected onChange: (value: string | number | null) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: string | number | null | undefined): void {
    this.value.set(value ?? null);
  }

  registerOnChange(fn: (value: string | number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(_isDisabled: boolean): void {}

  protected select(opt: SelectOption<string | number>): void {
    if (this.disabled()) return;
    this.value.set(opt.value);
    this.onChange(opt.value);
  }

  protected onBlur(): void {
    this.onTouched();
  }
}
