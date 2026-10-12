export type AdvancedFilterFieldType = 'text' | 'select' | 'date' | 'date-range' | 'resource';

export interface AdvancedFilterFieldConfig {
  key: string;
  label: string;
  type: AdvancedFilterFieldType;
  resourceKind?:string;
  placeholder?: string;
  options?: { value: string; label: string }[];
}

export type AdvancedFilterConfig = AdvancedFilterFieldConfig[];

export type AdvancedFilterModel = Record<string, string | number | null>;
