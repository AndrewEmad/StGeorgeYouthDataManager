import type { ValidatorFn, AsyncValidatorFn } from '@angular/forms';
import type { SelectOption } from './common.model';

export type DynamicFieldType =
  | 'text'
  | 'number'
  | 'select'
  | 'date'
  | 'checkbox'
  | 'radio'
  | 'textarea'
  | 'file';

export interface FormFieldConfig<T = unknown> {
  key: string;
  type: DynamicFieldType;
  label: string;
  placeholder?: string;
  validators?: ValidatorFn | ValidatorFn[];
  asyncValidators?: AsyncValidatorFn | AsyncValidatorFn[];
  options?: SelectOption<string | number>[];
  visibleWhen?: (formValue: Record<string, unknown>) => boolean;
  defaultValue?: T;
  /** For text: 'text' | 'email' | 'password' | 'tel' */
  inputType?: string;
  /** For date: 'date' | 'datetime-local' */
  dateMode?: 'date' | 'datetime-local';
  /** For number: min, max, step */
  min?: number;
  max?: number;
  step?: number;
  /** For textarea: rows */
  rows?: number;
  /** For select/radio: layout */
  layout?: 'horizontal' | 'vertical';
  required?: boolean;
  hint?: string;
}
