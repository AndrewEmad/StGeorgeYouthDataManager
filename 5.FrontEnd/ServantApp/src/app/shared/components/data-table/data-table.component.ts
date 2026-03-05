import {
  ChangeDetectionStrategy,
  Component,
  contentChildren,
  computed,
  input,
  output,
  signal,
  TemplateRef,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import type { ColumnDef, DataTableMode, SortDirection } from '../../models/data-table.model';
import { ServantColumnDirective } from '../../directives/servant-column.directive';

@Component({
  selector: 'servant-data-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.css',
})
export class DataTableComponent<T extends Record<string, unknown> = Record<string, unknown>> {
  /** Column definitions. */
  columns = input.required<ColumnDef[]>();

  /** Row data. */
  data = input.required<T[]>({ alias: 'data' });

  /** Loading state. */
  loading = input<boolean>(false);

  /** 'client' = paginate/sort in-memory; 'server' = emit pageChange/sortChange. */
  mode = input<DataTableMode>('client');

  /** Page size. */
  pageSize = input<number>(10);

  /** Total count (for server mode). */
  totalCount = input<number>(0);

  /** Empty state message. */
  emptyMessage = input<string>('لا توجد بيانات');

  /** Show export CSV button. */
  showExportButton = input<boolean>(false);

  /** Emitted when page or pageSize changes (server mode). */
  pageChange = output<{ page: number; pageSize: number }>();

  /** Emitted when sort column/direction changes (server mode). */
  sortChange = output<{ key: string; direction: SortDirection }>();

  protected page = signal(1);
  protected sortKey = signal<string | null>(null);
  protected sortDirection = signal<SortDirection>('asc');

  protected columnTemplates = contentChildren(ServantColumnDirective);

  protected readonly totalPages = computed(() => {
    const mode = this.mode();
    const total = mode === 'server' ? this.totalCount() : this.data().length;
    const size = this.pageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  protected readonly slicedData = computed(() => {
    if (this.mode() === 'server') return this.data();
    const all = this.sortedData();
    const size = this.pageSize();
    const p = this.page();
    const start = (p - 1) * size;
    return all.slice(start, start + size);
  });

  protected readonly sortedData = computed(() => {
    const key = this.sortKey();
    const dir = this.sortDirection();
    const list = [...this.data()];
    if (!key) return list;
    list.sort((a, b) => {
      const va = a[key];
      const vb = b[key];
      const cmp = va == null && vb == null ? 0 : String(va).localeCompare(String(vb), undefined, { numeric: true });
      return dir === 'asc' ? cmp : -cmp;
    });
    return list;
  });

  protected readonly displayCount = computed(() => {
    if (this.mode() === 'server') return this.totalCount();
    return this.data().length;
  });

  protected getColumnTemplate(name: string): TemplateRef<{ $implicit: T; row: T }> | null {
    const t = this.columnTemplates();
    const found = t?.find((c) => c.name() === name);
    return (found?.templateRef as TemplateRef<{ $implicit: T; row: T }>) ?? null;
  }

  protected setPage(p: number): void {
    const max = this.totalPages();
    const next = Math.max(1, Math.min(p, max));
    this.page.set(next);
    this.pageChange.emit({ page: next, pageSize: this.pageSize() });
  }

  protected toggleSort(col: ColumnDef): void {
    if (!col.sortable) return;
    const key = col.key;
    const current = this.sortKey();
    const dir = this.sortDirection();
    if (current === key) {
      this.sortDirection.set(dir === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortKey.set(key);
      this.sortDirection.set('asc');
    }
    this.sortChange.emit({ key: this.sortKey()!, direction: this.sortDirection() });
  }

  protected getCellValue(row: T, key: string): unknown {
    return row[key];
  }

  /** Export current data (or sliced data in client mode) to CSV. */
  exportToCsv(filename = 'export.csv'): void {
    const cols = this.columns();
    const rows = this.mode() === 'client' ? this.sortedData() : this.data();
    const headers = cols.map((c) => c.label);
    const lines = [headers.join(',')];
    for (const row of rows) {
      const cells = cols.map((c) => {
        const v = this.getCellValue(row as T, c.key);
        const s = v == null ? '' : String(v);
        return s.includes(',') || s.includes('"') ? `"${s.replace(/"/g, '""')}"` : s;
      });
      lines.push(cells.join(','));
    }
    const blob = new Blob(['\uFEFF' + lines.join('\n')], { type: 'text/csv;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = filename;
    a.click();
    URL.revokeObjectURL(a.href);
  }
}
