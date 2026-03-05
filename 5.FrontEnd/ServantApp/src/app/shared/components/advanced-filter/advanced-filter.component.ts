import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { TextInputComponent } from '../text-input/text-input.component';
import { SelectComponent } from '../select/select.component';
import { DatePickerComponent } from '../date-picker/date-picker.component';
import type { FilterFieldConfig, FilterModel } from '../../models/advanced-filter.model';

@Component({
  selector: 'servant-advanced-filter',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    TextInputComponent,
    SelectComponent,
    DatePickerComponent,
  ],
  templateUrl: './advanced-filter.component.html',
  styleUrl: './advanced-filter.component.css',
})
export class AdvancedFilterComponent {
  /** Filter field definitions. */
  fields = input.required<FilterFieldConfig[]>();

  /** Section title when collapsed. */
  filterTitle = input<string>('بحث متقدم');

  /** Sync filter to query params (e.g. ?search=x&area=y). */
  syncQueryParams = input<boolean>(false);

  /** Query param key prefix (e.g. 'filter_'). */
  queryParamPrefix = input<string>('');

  /** Emitted when user clicks Apply. */
  filterChange = output<FilterModel>();

  protected collapsed = signal(true);
  protected form = signal<FormGroup | null>(null);

  constructor(private readonly router: Router) {
    effect(() => {
      const fieldList = this.fields();
      const group = new FormGroup<Record<string, FormControl<unknown>>>({});
      for (const f of fieldList) {
        if (f.type === 'date-range') {
          group.addControl(`${f.key}_from`, new FormControl<string | null>(null));
          group.addControl(`${f.key}_to`, new FormControl<string | null>(null));
        } else if (f.type === 'multi-select') {
          group.addControl(f.key, new FormControl<(string | number)[]>([]));
        } else {
          group.addControl(f.key, new FormControl<string | number | null>(null));
        }
      }
      this.form.set(group);
    });
  }

  protected toggle(): void {
    this.collapsed.update((c) => !c);
  }

  protected apply(): void {
    const g = this.form();
    if (!g) return;
    const raw = g.getRawValue() as Record<string, unknown>;
    const model: FilterModel = {};
    for (const f of this.fields()) {
      if (f.type === 'date-range') {
        model[f.key] = {
          from: (raw[`${f.key}_from`] as string | null) ?? null,
          to: (raw[`${f.key}_to`] as string | null) ?? null,
        };
      } else {
        const v = raw[f.key];
        model[f.key] = v === '' || v === undefined ? null : (v as FilterModel[string]);
      }
    }
    this.filterChange.emit(model);
    if (this.syncQueryParams()) {
      this.applyToQueryParams(model);
    }
  }

  protected reset(): void {
    const g = this.form();
    if (!g) return;
    g.reset();
    for (const f of this.fields()) {
      if (f.type === 'multi-select') {
        g.get(f.key)?.setValue([]);
      }
      if (f.type === 'date-range') {
        g.get(`${f.key}_from`)?.setValue(null);
        g.get(`${f.key}_to`)?.setValue(null);
      }
    }
    this.filterChange.emit({});
    if (this.syncQueryParams()) {
      this.router.navigate([], { queryParams: {} });
    }
  }

  protected toggleMultiSelect(key: string, value: string | number): void {
    const g = this.form();
    if (!g) return;
    const c = g.get(key);
    if (!c) return;
    const arr = (c.value as (string | number)[]) ?? [];
    const idx = arr.indexOf(value);
    const next = idx === -1 ? [...arr, value] : arr.filter((_, i) => i !== idx);
    c.setValue(next);
  }

  protected isMultiSelected(key: string, value: string | number): boolean {
    const g = this.form();
    if (!g) return false;
    const arr = (g.get(key)?.value as (string | number)[]) ?? [];
    return arr.includes(value);
  }

  private applyToQueryParams(model: FilterModel): void {
    const prefix = this.queryParamPrefix();
    const params: Record<string, string> = {};
    for (const [k, v] of Object.entries(model)) {
      if (v == null) continue;
      const key = prefix ? `${prefix}${k}` : k;
      if (typeof v === 'object' && !Array.isArray(v) && 'from' in v) {
        if (v.from) params[`${key}_from`] = v.from;
        if (v.to) params[`${key}_to`] = v.to;
      } else if (Array.isArray(v)) {
        params[key] = v.join(',');
      } else {
        params[key] = String(v);
      }
    }
    this.router.navigate([], { queryParams: params, queryParamsHandling: 'merge' });
  }
}
