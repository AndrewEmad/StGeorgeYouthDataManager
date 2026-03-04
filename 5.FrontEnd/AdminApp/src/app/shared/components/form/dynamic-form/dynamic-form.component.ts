import {
  Component,
  input,
  output,
  signal,
  computed,
  ChangeDetectionStrategy,
  inject,
} from '@angular/core';
import {
  FormGroup,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { DynamicFormConfig, DynamicFormFieldConfig } from './dynamic-form-config';
import { TextInputComponent } from '../text-input/text-input.component';
import { NumberInputComponent } from '../number-input/number-input.component';
import { SelectComponent } from '../select/select.component';
import { DatePickerComponent } from '../date-picker/date-picker.component';
import { TextareaComponent } from '../textarea/textarea.component';
import { CheckboxComponent } from '../checkbox/checkbox.component';
import { RadioGroupComponent } from '../radio-group/radio-group.component';

@Component({
  selector: 'admin-dynamic-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TextInputComponent,
    NumberInputComponent,
    SelectComponent,
    DatePickerComponent,
    TextareaComponent,
    CheckboxComponent,
    RadioGroupComponent,
  ],
  templateUrl: './dynamic-form.component.html',
  styleUrl: './dynamic-form.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DynamicFormComponent {
  private fb = inject(FormBuilder);

  config = input.required<DynamicFormConfig>();
  submitLabel = input<string>('حفظ');

  formSubmit = output<Record<string, unknown>>();

  form = signal<FormGroup | null>(null);
  formValue = signal<Record<string, unknown>>({});

  visibleConfig = computed(() => {
    const cfg = this.config();
    const value = this.formValue();
    return cfg.filter((f) => {
      if (!f.visibleWhen) return true;
      return f.visibleWhen(value);
    });
  });

  ngOnInit(): void {
    const cfg = this.config();
    const group: Record<string, unknown> = {};
    for (const field of cfg) {
      const validators = field.required ? [Validators.required] : [];
      if (field.validators) validators.push(...field.validators);
      let value: unknown = field.value;
      if (value === undefined) {
        if (field.type === 'checkbox') value = false;
        else if (field.type === 'number') value = null;
        else value = '';
      }
      group[field.key] = [value, validators];
    }
    const grp = this.fb.group(group);
    this.form.set(grp);
    this.formValue.set(grp.getRawValue());
    grp.valueChanges.subscribe((v) => this.formValue.set(grp.getRawValue()));
  }

  getForm(): FormGroup | null {
    return this.form();
  }

  onSubmit(): void {
    const grp = this.form();
    if (!grp || grp.invalid) return;
    this.formSubmit.emit(grp.getRawValue());
  }

  trackByKey(_index: number, field: DynamicFormFieldConfig): string {
    return field.key;
  }
}
