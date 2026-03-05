/**
 * Column definition for servant-data-table.
 */
export interface ColumnDef<T = unknown> {
  /** Property key on the row object, or template name for custom cell. */
  key: string;
  /** Header label. */
  label: string;
  /** Enable sorting for this column. */
  sortable?: boolean;
  /** CSS width (e.g. '100px', '20%'). */
  width?: string;
  /** Name of the ng-template (servantColumn) to use for cell content. */
  template?: string;
}

/**
 * Pagination mode.
 */
export type DataTableMode = 'client' | 'server';

/**
 * Sort direction.
 */
export type SortDirection = 'asc' | 'desc';
