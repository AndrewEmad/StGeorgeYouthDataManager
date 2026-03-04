import { ValidatorFn } from '@angular/forms';

export type DynamicFieldType =
  | 'text'
  | 'number'
  | 'select'
  | 'date'
  | 'textarea'
  | 'checkbox'
  | 'radio';

export interface DynamicFormFieldConfig {
  key: string;
  label: string;
  type: DynamicFieldType;
  placeholder?: string;
  required?: boolean;
  value?: unknown;
  options?: { value: string; label: string }[];
  validators?: ValidatorFn[];
  visibleWhen?: (formValue: Record<string, unknown>) => boolean;
}

export type DynamicFormConfig = DynamicFormFieldConfig[];
