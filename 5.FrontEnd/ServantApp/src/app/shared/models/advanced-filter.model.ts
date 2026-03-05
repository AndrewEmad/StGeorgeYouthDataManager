import type { SelectOption } from './common.model';

export type FilterFieldType = 'text' | 'select' | 'date' | 'date-range' | 'multi-select';

export interface FilterFieldConfig {
  key: string;
  label: string;
  type: FilterFieldType;
  placeholder?: string;
  options?: SelectOption<string | number>[];
}

/**
 * Emitted filter model: key -> value.
 * For date-range fields, value is { from: string | null, to: string | null }.
 * For multi-select, value is (string | number)[].
 */
export type FilterModel = Record<string, string | number | (string | number)[] | { from: string | null; to: string | null } | null>;
