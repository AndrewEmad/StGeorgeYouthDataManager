import {
  Component,
  input,
  output,
  ChangeDetectionStrategy,
  TemplateRef,
} from '@angular/core';
import { DataTableColumn } from '../data-table/data-table-column';
import { AdvancedFilterConfig, AdvancedFilterModel } from '../advanced-filter/advanced-filter-config';
import { DataTableComponent } from '../data-table/data-table.component';
import { AdvancedFilterComponent } from '../advanced-filter/advanced-filter.component';
import { ContentHeaderComponent } from '../../../components/common/content-header/content-header.component';

@Component({
  selector: 'admin-report-viewer',
  standalone: true,
  imports: [
    ContentHeaderComponent,
    AdvancedFilterComponent,
    DataTableComponent,
  ],
  templateUrl: './report-viewer.component.html',
  styleUrl: './report-viewer.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportViewerComponent<T extends object = Record<string, unknown>> {
  title = input<string>('');
  subtitle = input<string>('');
  backLink = input<string>('');
  backLabel = input<string>('العودة');

  filterConfig = input<AdvancedFilterConfig>([]);
  /** Optional initial filter values (e.g. default days) */
  initialFilterModel = input<AdvancedFilterModel | null>(null);
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
  actionsTemplate = input<TemplateRef<{ $implicit: T }> | null>(null);

  filterChange = output<AdvancedFilterModel>();
  pageChange = output<number>();
  pageSizeChange = output<number>();
  sortChange = output<{ column: string; desc: boolean }>();
  rowClick = output<T>();
}
