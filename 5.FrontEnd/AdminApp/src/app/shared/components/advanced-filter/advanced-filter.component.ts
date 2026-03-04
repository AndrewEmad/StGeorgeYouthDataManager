import {
  Component,
  input,
  output,
  signal,
  computed,
  ChangeDetectionStrategy,
  inject,
  effect,
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdvancedFilterConfig, AdvancedFilterModel } from './advanced-filter-config';
import { FiltersBarComponent } from '../../../components/common/filters-bar/filters-bar.component';

@Component({
  selector: 'admin-advanced-filter',
  standalone: true,
  imports: [FiltersBarComponent],
  templateUrl: './advanced-filter.component.html',
  styleUrl: './advanced-filter.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdvancedFilterComponent {
  private router = inject(Router);

  config = input.required<AdvancedFilterConfig>();
  initialModel = input<AdvancedFilterModel | null>(null);
  toggleLabel = input<string>('بحث متقدم');
  /** If set, sync filter state with these query param keys */
  queryParamKeys = input<string[]>([]);

  filterChange = output<AdvancedFilterModel>();

  open = signal(false);
  model = signal<AdvancedFilterModel>({});

  private keysSet = computed(() => {
    const cfg = this.config();
    return new Set(cfg.map((f) => f.key));
  });

  constructor() {
    effect(() => {
      const initial = this.initialModel();
      const keys = this.keysSet();
      const next: AdvancedFilterModel = {};
      for (const k of keys) {
        next[k] = initial?.[k] ?? null;
      }
      this.model.set(next);
    });
  }

  ngOnInit(): void {
    const keys = this.queryParamKeys();
    if (keys.length === 0) return;
    const params = this.router.parseUrl(this.router.url).queryParams;
    const fromParams: AdvancedFilterModel = {};
    for (const k of keys) {
      const v = params[k];
      fromParams[k] = v ?? null;
    }
    this.model.update((m) => ({ ...m, ...fromParams }));
  }

  toggleOpen(): void {
    this.open.update((v) => !v);
  }

  getValue(key: string): string | null {
    const v = this.model()[key];
    return v == null ? null : String(v);
  }

  setValue(key: string, value: string | null): void {
    this.model.update((m) => ({ ...m, [key]: value }));
    this.emitFilter();
    this.syncQueryParams();
  }

  apply(): void {
    this.emitFilter();
    this.syncQueryParams();
  }

  reset(): void {
    const cfg = this.config();
    const next: AdvancedFilterModel = {};
    for (const f of cfg) {
      next[f.key] = null;
      if (f.type === 'date-range') {
        next[f.key + '_from'] = null;
        next[f.key + '_to'] = null;
      }
    }
    this.model.set(next);
    this.emitFilter();
    this.syncQueryParams();
  }

  private emitFilter(): void {
    this.filterChange.emit({ ...this.model() });
  }

  private syncQueryParams(): void {
    const keys = this.queryParamKeys();
    if (keys.length === 0) return;
    const m = this.model();
    const params: Record<string, string> = {};
    for (const k of keys) {
      const v = m[k];
      if (v != null && v !== '') {
        params[k] = String(v);
      }
    }
    this.router.navigate([], {
      queryParams: params,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  trackByKey(_index: number, field: { key: string }): string {
    return field.key;
  }
}
