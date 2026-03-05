import type { FilterFieldConfig } from './advanced-filter.model';
import type { ColumnDef } from './data-table.model';

/**
 * Configuration for servant-report-viewer: filters + table columns.
 */
export interface ReportViewerConfig<T = Record<string, unknown>> {
  /** Filter field definitions. */
  filterFields: FilterFieldConfig[];
  /** Table column definitions. */
  columns: ColumnDef<T>[];
  /** Optional report title (e.g. for print). */
  title?: string;
  /** Empty state message when no data. */
  emptyMessage?: string;
}
