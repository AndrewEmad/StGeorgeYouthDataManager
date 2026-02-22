import { Component, input, output, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.css'
})
export class PaginationComponent {
  total = input.required<number>();
  page = input.required<number>();
  pageSize = input.required<number>();
  pageSizeOptions = input<number[]>([10, 25, 50]);
  /** Number of items on current page (for "from - to" display) */
  currentPageCount = input.required<number>();
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  from = computed(() => (this.page() - 1) * this.pageSize() + 1);
  to = computed(() => (this.page() - 1) * this.pageSize() + this.currentPageCount());
  pageChange = output<number>();
  pageSizeChange = output<number>();
}
