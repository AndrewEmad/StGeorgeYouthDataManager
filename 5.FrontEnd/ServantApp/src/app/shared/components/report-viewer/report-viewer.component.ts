import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { AdvancedFilterComponent } from '../advanced-filter/advanced-filter.component';
import { DataTableComponent } from '../data-table/data-table.component';
import type { FilterModel } from '../../models/advanced-filter.model';
import type { ColumnDef } from '../../models/data-table.model';
import type { FilterFieldConfig } from '../../models/advanced-filter.model';

@Component({
  selector: 'servant-report-viewer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AdvancedFilterComponent, DataTableComponent],
  templateUrl: './report-viewer.component.html',
  styleUrl: './report-viewer.component.css',
})
export class ReportViewerComponent<T extends Record<string, unknown> = Record<string, unknown>> {
  /** Filter field definitions. */
  filterFields = input.required<FilterFieldConfig[]>();

  /** Table column definitions. */
  columns = input.required<ColumnDef[]>();

  /** Row data to display. */
  data = input.required<T[]>({ alias: 'data' });

  /** Loading state. */
  loading = input<boolean>(false);

  /** Report title (e.g. for print header). */
  reportTitle = input<string>('تقرير');

  /** Empty state message. */
  emptyMessage = input<string>('لا توجد بيانات');

  /** Filter section title. */
  filterTitle = input<string>('بحث متقدم');

  /** Sync filter to URL query params. */
  syncQueryParams = input<boolean>(false);

  /** Emitted when user applies filters (parent can fetch data). */
  filterChange = output<FilterModel>();

  protected tableRef = viewChild(DataTableComponent);

  protected onFilterChange(model: FilterModel): void {
    this.filterChange.emit(model);
  }

  /** Export current data to CSV (delegates to table). */
  exportToCsv(filename?: string): void {
    this.tableRef()?.exportToCsv(filename);
  }

  /** Print the report (table + optional title). */
  print(): void {
    window.print();
  }
}
