import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

/**
 * Reusable form field wrapper: label, required indicator, slot for control,
 * error message(s), and optional hint. Use with CVA form controls or any input.
 */
@Component({
  selector: 'servant-form-field-wrapper',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-field-wrapper.component.html',
  styleUrl: './form-field-wrapper.component.css',
})
export class FormFieldWrapperComponent {
  /** Label text (required for accessibility). */
  label = input.required<string>();

  /** Id for the control (used for label[for] and aria-describedby). */
  controlId = input<string | null>(null);

  /** Show required asterisk. */
  required = input<boolean>(false);

  /** Single error message or null. */
  error = input<string | null>(null);

  /** Multiple error messages (shown as list if provided). */
  errors = input<string[]>([]);

  /** Hint text below the control. */
  hint = input<string | null>(null);

  /** Whether the control is invalid (e.g. for aria-invalid). */
  invalid = input<boolean>(false);

  /** Combined list of error messages for template. */
  readonly allErrors = computed(() => {
    const single = this.error();
    const multiple = this.errors();
    if (single) return [single];
    return multiple?.length ? multiple : [];
  });

  /** Id for the error element (for aria-describedby). */
  readonly errorId = computed(() => {
    const id = this.controlId();
    return id ? `${id}-error` : null;
  });

  /** Id for the hint element (for aria-describedby). */
  readonly hintId = computed(() => {
    const id = this.controlId();
    return id ? `${id}-hint` : null;
  });
}
