import {
  Component,
  input,
  output,
  ChangeDetectionStrategy,
  TemplateRef,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { DataTableColumn } from './data-table-column';
import { PaginationComponent } from '../../../components/common/pagination/pagination.component';
import { LoaderComponent } from '../../../components/common/loader/loader.component';
import { EmptyStateComponent } from '../../../components/common/empty-state/empty-state.component';
import { DateFormatPipe } from '../../pipes/date-format.pipe';
import { MonthNamePipe } from '../../pipes/month-name.pipe';

@Component({
  selector: 'admin-data-table',
  standalone: true,
  imports: [NgTemplateOutlet, DateFormatPipe, MonthNamePipe, PaginationComponent, LoaderComponent, EmptyStateComponent],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataTableComponent<T extends object = Record<string, unknown>> {
  columns = input.required<DataTableColumn<T>[]>();
  data = input.required<T[]>();
  loading = input<boolean>(false);
  totalCount = input<number>(0);
  page = input<number>(1);
  pageSize = input<number>(10);
  pageSizeOptions = input<number[]>([5, 10, 20, 50]);
  sortBy = input<string | null>(null);
  sortDesc = input<boolean>(false);
  emptyMessage = input<string>('لا توجد بيانات');
  rowClickable = input<boolean>(true);
  /** Optional template for row actions column */
  actionsTemplate = input<TemplateRef<{ $implicit: T }> | null>(null);

  pageChange = output<number>();
  pageSizeChange = output<number>();
  sortChange = output<{ column: string; desc: boolean }>();
  rowClick = output<T>();

  getValue(row: T, key: string): unknown {
    return (row as Record<string, unknown>)[key];
  }

  onSort(column: string): void {
    const current = this.sortBy();
    const desc = current === column ? !this.sortDesc() : false;
    this.sortChange.emit({ column, desc });
  }

  trackByKey(_index: number, col: DataTableColumn<T>): string {
    return col.key;
  }

  trackByIndex(index: number): number {
    return index;
  }

  exportCsv(): void {
    const cols = this.columns();
    const rows = this.data();
    const headers = cols.map((c) => c.header).join(',');
    const escape = (v: unknown) => {
      const s = v == null ? '' : String(v);
      return s.includes(',') || s.includes('"') ? `"${s.replace(/"/g, '""')}"` : s;
    };
    const lines = rows.map((row) =>
      cols.map((c) => escape(this.getValue(row, c.key))).join(',')
    );
    const csv = ['\ufeff', headers, ...lines].join('\r\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `export-${Date.now()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
