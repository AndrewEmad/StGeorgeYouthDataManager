import { TemplateRef } from '@angular/core';

export interface DataTableColumn<T = unknown> {
  key: keyof T & string;
  header: string;
  sortable?: boolean;
  /** Optional: 'date' uses DateFormatPipe, 'month' uses MonthNamePipe for display */
  format?: 'date' | 'month';
  /** Optional template for cell content. Receives row and value. */
  template?: TemplateRef<{ $implicit: T; value: unknown }>;
}
