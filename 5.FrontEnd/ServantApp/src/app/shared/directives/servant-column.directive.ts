import { Directive, inject, input, TemplateRef } from '@angular/core';

/**
 * Place on ng-template to define a custom column cell for servant-data-table.
 * Use with [servantColumn]="'columnName'" and let-row in the template.
 */
@Directive({
  selector: '[servantColumn]',
  standalone: true,
})
export class ServantColumnDirective {
  readonly templateRef = inject(TemplateRef<{ $implicit: unknown; row: unknown }>);
  /** Name that matches ColumnDef.template. */
  readonly name = input.required<string>({ alias: 'servantColumn' });
}
