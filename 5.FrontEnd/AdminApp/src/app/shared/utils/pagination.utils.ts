import { signal, computed, WritableSignal, Signal } from '@angular/core';

export interface UsePaginationReturn {
  page: WritableSignal<number>;
  pageSize: WritableSignal<number>;
  totalCount: WritableSignal<number>;
  sortBy: WritableSignal<string | null>;
  sortDesc: WritableSignal<boolean>;
  totalPages: Signal<number>;
  pageSizeOptions: number[];
  goToPage: (p: number) => void;
  setPageSize: (n: number) => void;
  setSort: (column: string) => void;
}

/**
 * Composable for pagination and sorting state. Call loadFn when page/size/sort changes.
 * Component should set totalCount after each load.
 */
export function usePagination(loadFn: () => void, options?: { pageSizeOptions?: number[] }): UsePaginationReturn {
  const page = signal(1);
  const pageSize = signal(10);
  const totalCount = signal(0);
  const sortBy = signal<string | null>(null);
  const sortDesc = signal(false);
  const pageSizeOptions = options?.pageSizeOptions ?? [5, 10, 20, 50];

  const totalPages = computed(() => Math.max(1, Math.ceil(totalCount() / pageSize())));

  function goToPage(p: number): void {
    const max = totalPages();
    if (p < 1 || p > max) return;
    page.set(p);
    loadFn();
  }

  function setPageSize(n: number): void {
    pageSize.set(Number(n));
    page.set(1);
    loadFn();
  }

  function setSort(column: string): void {
    if (sortBy() === column) {
      sortDesc.update((v) => !v);
    } else {
      sortBy.set(column);
      sortDesc.set(false);
    }
    page.set(1);
    loadFn();
  }

  return {
    page,
    pageSize,
    totalCount,
    sortBy,
    sortDesc,
    totalPages,
    pageSizeOptions,
    goToPage,
    setPageSize,
    setSort,
  };
}
