/**
 * Generic paged API result.
 */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/**
 * Option for select/radio/dropdown components.
 */
export interface SelectOption<T = string | number> {
  value: T;
  label: string;
}

/**
 * Common API response with id only.
 */
export interface IdResponse {
  id: string;
}
