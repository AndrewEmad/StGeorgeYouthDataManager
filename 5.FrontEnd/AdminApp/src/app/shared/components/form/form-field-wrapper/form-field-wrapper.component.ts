import { Component, input } from '@angular/core';

@Component({
  selector: 'admin-form-field-wrapper',
  standalone: true,
  templateUrl: './form-field-wrapper.component.html',
  styleUrl: './form-field-wrapper.component.css',
})
export class FormFieldWrapperComponent {
  label = input<string>('');
  id = input<string>('');
  error = input<string | null>(null);
  required = input<boolean>(false);
}
