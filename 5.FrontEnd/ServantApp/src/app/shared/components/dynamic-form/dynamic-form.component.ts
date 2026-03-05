import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import type { ValidatorFn, AsyncValidatorFn } from '@angular/forms';
import { TextInputComponent } from '../text-input/text-input.component';
import { NumberInputComponent } from '../number-input/number-input.component';
import { SelectComponent } from '../select/select.component';
import { DatePickerComponent } from '../date-picker/date-picker.component';
import { CheckboxComponent } from '../checkbox/checkbox.component';
import { RadioGroupComponent } from '../radio-group/radio-group.component';
import { TextareaComponent } from '../textarea/textarea.component';
import { FileUploadComponent } from '../file-upload/file-upload.component';
import type { FormFieldConfig } from '../../models/dynamic-form.model';

@Component({
  selector: 'servant-dynamic-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    TextInputComponent,
    NumberInputComponent,
    SelectComponent,
    DatePickerComponent,
    CheckboxComponent,
    RadioGroupComponent,
    TextareaComponent,
    FileUploadComponent,
  ],
  templateUrl: './dynamic-form.component.html',
  styleUrl: './dynamic-form.component.css',
})
export class DynamicFormComponent {
  /** Field definitions (JSON config). */
  fields = input.required<FormFieldConfig[]>();

  /** Submit button label. */
  submitLabel = input<string>('حفظ');

  /** Emitted when form is submitted and valid. */
  formSubmit = output<Record<string, unknown>>();

  protected form = signal<FormGroup | null>(null);
  protected formValue = signal<Record<string, unknown>>({});

  protected visibleFields = computed(() => {
    const f = this.form();
    const fields = this.fields();
    const value = this.formValue();
    if (!f || !fields.length) return [];
    return fields.filter((field) => !field.visibleWhen || field.visibleWhen(value));
  });

  constructor() {
    effect(() => {
      const fieldList = this.fields();
      const group = this.buildFormGroup(fieldList);
      this.form.set(group);
      this.formValue.set(group.getRawValue());
      const sub = group.valueChanges.subscribe((v) => this.formValue.set(v));
      return () => sub.unsubscribe();
    });
  }

  private buildFormGroup(fieldList: FormFieldConfig[]): FormGroup {
    const controls: Record<string, FormControl<unknown>> = {};
    for (const field of fieldList) {
      const validators = this.normalizeValidators(field.validators, field.required);
      const asyncValidators = this.normalizeAsyncValidators(field.asyncValidators);
      const value = field.defaultValue ?? this.defaultForType(field.type);
      controls[field.key] = new FormControl(value, { validators, asyncValidators });
    }
    return new FormGroup(controls);
  }

  private defaultForType(type: FormFieldConfig['type']): unknown {
    switch (type) {
      case 'checkbox':
        return false;
      case 'number':
        return null;
      case 'file':
        return null;
      default:
        return '';
    }
  }

  private normalizeValidators(
    validators?: FormFieldConfig['validators'],
    required?: boolean
  ): ValidatorFn[] {
    const list: ValidatorFn[] = [];
    if (required) list.push(Validators.required);
    if (validators) list.push(...(Array.isArray(validators) ? validators : [validators]));
    return list;
  }

  private normalizeAsyncValidators(
    asyncValidators?: FormFieldConfig['asyncValidators']
  ): AsyncValidatorFn[] | null {
    if (!asyncValidators) return null;
    return Array.isArray(asyncValidators) ? asyncValidators : [asyncValidators];
  }

  protected onSubmit(): void {
    const f = this.form();
    if (!f) return;
    f.markAllAsTouched();
    if (f.valid) {
      this.formSubmit.emit(f.getRawValue());
    }
  }

  protected errorFor(key: string): string | null {
    const f = this.form();
    if (!f) return null;
    const c = f.get(key);
    if (!c?.errors || !c.touched) return null;
    const err = c.errors;
    if (err['required']) return 'مطلوب';
    if (err['email']) return 'البريد الإلكتروني غير صالح';
    if (err['min']) return `الحد الأدنى ${err['min'].min}`;
    if (err['max']) return `الحد الأقصى ${err['max'].max}`;
    return err['message'] ?? null;
  }
}
