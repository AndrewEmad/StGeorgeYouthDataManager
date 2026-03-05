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
  selector: 'servant-select',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldWrapperComponent],
  templateUrl: './select.component.html',
  styleUrl: './select.component.css',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: SelectComponent, multi: true },
  ],
})
export class SelectComponent implements ControlValueAccessor {
  private readonly uniqueId = `select-${Math.random().toString(36).slice(2, 10)}`;

  label = input.required<string>();
  controlId = input<string | null>(null);
  options = input.required<SelectOption<string | number>[]>();
  placeholder = input<string>('');
  required = input<boolean>(false);
  error = input<string | null>(null);
  hint = input<string | null>(null);
  disabled = input<boolean>(false);

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

  protected onChangeEvent(e: Event): void {
    const el = e.target as HTMLSelectElement;
    const raw = el.value;
    if (raw === '') {
      this.value.set(null);
      this.onChange(null);
      return;
    }
    const opt = this.options().find((o) => String(o.value) === raw);
    const v = opt?.value ?? null;
    this.value.set(v);
    this.onChange(v);
  }

  protected onBlur(): void {
    this.onTouched();
  }

  protected stringVal(v: string | number | null): string {
    return v != null ? `${v}` : '';
  }
}
