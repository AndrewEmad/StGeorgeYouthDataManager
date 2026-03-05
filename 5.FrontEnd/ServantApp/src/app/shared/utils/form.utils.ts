import { FormGroup, FormControl, AbstractControl, Validators, ValidatorFn } from '@angular/forms';

/**
 * Typed form group helper: builds a FormGroup with inferred type from default value.
 * Use for reactive forms with strict typing.
 */
export function buildFormGroup<T extends Record<string, unknown>>(
  defaults: T,
  validators?: ValidatorFn | ValidatorFn[] | null
): FormGroup<{ [K in keyof T]: FormControl<T[K]> }> {
  const controls: Record<string, FormControl<unknown>> = {};
  for (const key of Object.keys(defaults)) {
    controls[key] = new FormControl(defaults[key as keyof T], { nonNullable: true });
  }
  return new FormGroup(controls as { [K in keyof T]: FormControl<T[K]> }, validators ?? null);
}

/**
 * Common validator: required (non-empty string).
 */
export const required = () => Validators.required;

/**
 * Marks a control as touched and updates value and validity.
 * Use after submit to show validation errors.
 */
export function markGroupTouched(group: FormGroup | AbstractControl): void {
  group.markAllAsTouched();
  if (group instanceof FormGroup) {
    Object.values(group.controls).forEach((c) => markGroupTouched(c));
  }
}
