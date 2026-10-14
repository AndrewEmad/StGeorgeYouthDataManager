import { Component, DestroyRef, ElementRef, computed, effect, inject, input, output, signal, untracked, viewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, Subscription, debounceTime } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { YDM_API, apiError } from '../core/api';

export interface Item { id: string; name: string; }
let nextPickerId = 0;

@Component({
  selector: 'ydm-resource-picker', standalone: true,
  host: { '(document:pointerdown)': 'outsidePointer($event)', '(focusout)': 'focusOut($event)', '(keydown.escape)': 'escape($event)' },
  template: `
    <div class="ydm-picker">
      <label [for]="pickerId">{{label()}}</label>
      <div class="ydm-picker-control">
        <input #input class="form-input" [id]="pickerId" role="combobox" type="text" autocomplete="off"
          aria-autocomplete="list" aria-haspopup="listbox" [attr.aria-expanded]="opened()"
          [attr.aria-controls]="opened() ? pickerId + '-list' : null"
          [attr.aria-activedescendant]="activeOptionId()" [attr.aria-describedby]="opened() ? pickerId + '-status' : null"
          [placeholder]="opened() ? 'بحث بالاسم' : emptyText()" [disabled]="disabled()"
          [value]="opened() ? search : displayName()" (focus)="open()" (click)="open()"
          (input)="searchChanged($any($event.target).value)" (keydown)="keydown($event)" />
        <button class="ydm-picker-toggle" type="button" [disabled]="disabled()" tabindex="-1"
          [attr.aria-label]="(opened() ? 'إغلاق قائمة ' : 'فتح قائمة ') + label()"
          (pointerdown)="$event.preventDefault()" (click)="toggle()">
          <span class="material-icons" aria-hidden="true">{{opened() ? 'expand_less' : 'expand_more'}}</span>
        </button>
      </div>
      @if(opened()){
        <div class="ydm-picker-panel">
          <div #list class="ydm-picker-list" [id]="pickerId + '-list'" role="listbox" tabindex="-1" [attr.aria-label]="label()"
            [attr.aria-busy]="loading()" (scroll)="onScroll($event)">
            @for(item of options(); track item.id; let index = $index){
              <div class="ydm-picker-option" [id]="pickerId + '-option-' + index" role="option"
                [attr.aria-selected]="item.id === (value() || '')" [class.ydm-picker-active]="index === activeIndex()"
                (pointerdown)="$event.preventDefault()" (click)="choose(item)">
                <span>{{item.name}}</span>
                @if(item.id === (value() || '')){<span class="material-icons" aria-hidden="true">check</span>}
              </div>
            }
          </div>
          @if(value()){<small class="ydm-picker-status">الاختيار الحالي: {{displayName()}}</small>}
          <div class="ydm-picker-status" [id]="pickerId + '-status'" role="status">
            @if(loading()){<small>جارٍ تحميل الخيارات…</small>}
            @else if(!error() && !items().length){<small>لا توجد نتائج مطابقة.</small>}
            @else if(!error()){<small>عرض {{items().length}} من {{total()}} خيار{{hasMore() ? ' · مرّر لعرض المزيد' : ''}}</small>}
          </div>
          @if(error()){
            <div class="ydm-picker-status"><small class="text-error" role="alert">{{error()}}</small>
              <button class="btn-outline" type="button" (click)="retry()" [disabled]="disabled() || loading()">إعادة المحاولة</button>
            </div>
          }
        </div>
      }
    </div>`
})
export class ResourcePickerComponent {
  kind = input.required<string>(); label = input.required<string>(); value = input<string | null>(null);
  disabled = input(false); allowEmpty = input(false); emptyLabel = input(''); selectedLabel = input('');
  valueChange = output<string | null>(); itemChange = output<Item | null>();
  readonly pickerId = `ydm-picker-${++nextPickerId}`;
  private api = inject(YDM_API); private http = inject(HttpClient); private destroy = inject(DestroyRef);
  private element = inject(ElementRef<HTMLElement>);
  private inputElement = viewChild<ElementRef<HTMLInputElement>>('input');
  private listElement = viewChild<ElementRef<HTMLElement>>('list');
  private request?: Subscription; private terms = new Subject<number>(); private queryVersion = 0;
  private loadedPage = 0; private failedPage = 1; private appliedSearch = ''; private advanceAfterLoad = false;
  private selected = signal<Item | null>(null);
  search = ''; items = signal<Item[]>([]); total = signal(0); loading = signal(false); error = signal('');
  opened = signal(false); hasMore = signal(true); activeIndex = signal(-1);
  emptyText = computed(() => this.emptyLabel() || (this.allowEmpty() ? 'غير مسند' : 'اختر من القائمة'));
  displayName = computed(() => {
    const id = this.value();
    if (!id) return '';
    return this.items().find(item => item.id === id)?.name
      || (this.selected()?.id === id ? this.selected()!.name : '') || this.selectedLabel() || 'الاختيار الحالي';
  });
  options = computed(() => {
    const rows = [...this.items()];
    if (this.value() && !rows.some(item => item.id === this.value())) rows.unshift({id: this.value()!, name: this.displayName()});
    if (this.allowEmpty()) rows.unshift({id: '', name: this.emptyText()});
    return rows;
  });
  activeOptionId = computed(() => this.opened() && this.options()[this.activeIndex()] ? `${this.pickerId}-option-${this.activeIndex()}` : null);

  constructor() {
    effect(() => { this.kind(); untracked(() => { this.search = ''; this.selected.set(null); this.reset(false); this.loadPage(1); }); });
    effect(() => { if (this.disabled()) untracked(() => this.close()); });
    this.terms.pipe(debounceTime(300), takeUntilDestroyed(this.destroy)).subscribe(version => { if (version === this.queryVersion) this.loadPage(1); });
    this.destroy.onDestroy(() => this.request?.unsubscribe());
  }
  open(): void {
    if (this.disabled() || this.opened()) return;
    this.opened.set(true);
    this.activeIndex.set(this.options().findIndex(item => item.id === (this.value() || '')));
    this.revealActive();
  }
  close(): void { this.opened.set(false); this.activeIndex.set(-1); }
  escape(event: Event): void {
    if (this.opened()) { event.preventDefault(); event.stopPropagation(); this.close(); }
  }
  toggle(): void {
    if (this.opened()) this.close();
    else { this.inputElement()?.nativeElement.focus(); this.open(); }
  }
  outsidePointer(event: PointerEvent): void {
    if (event.target instanceof Node && !this.element.nativeElement.contains(event.target)) this.close();
  }
  focusOut(event: FocusEvent): void {
    if (!(event.relatedTarget instanceof Node) || !this.element.nativeElement.contains(event.relatedTarget)) this.close();
  }
  choose(item: Item): void {
    if (this.disabled()) return;
    this.selected.set(item.id ? item : null);
    this.valueChange.emit(item.id || null); this.itemChange.emit(item.id ? item : null);
    this.close(); this.inputElement()?.nativeElement.focus({preventScroll: true});
    // Focusing from the retry button may open it again; selection always closes the list.
    this.close();
  }
  searchChanged(value: string): void {
    if (this.disabled()) return;
    this.search = value; this.opened.set(true); this.reset(); this.loading.set(true); this.terms.next(this.queryVersion);
  }
  private reset(preserveSelection = true): void {
    if (preserveSelection) { const current = this.items().find(item => item.id === this.value()); if (current) this.selected.set(current); }
    this.queryVersion++;
    this.request?.unsubscribe(); this.loadedPage = 0; this.appliedSearch = this.search;
    this.items.set([]); this.total.set(0); this.error.set(''); this.hasMore.set(true);
    this.activeIndex.set(-1); this.advanceAfterLoad = false;
    const list = this.listElement()?.nativeElement; if (list) list.scrollTop = 0;
  }
  onScroll(event: Event): void {
    const list = event.target as HTMLElement;
    if (list.scrollHeight - list.scrollTop - list.clientHeight <= 48) this.loadMore();
  }
  private loadMore(): void {
    if (this.disabled() || this.loading() || this.error() || !this.hasMore()) return;
    this.loadPage(this.loadedPage + 1);
  }
  retry(): void {
    if (this.disabled() || this.loading()) return;
    // The retry button disappears when loading begins; keep focus in the picker first.
    this.inputElement()?.nativeElement.focus({preventScroll: true}); this.loadPage(this.failedPage);
  }
  private loadPage(page: number): void {
    this.request?.unsubscribe(); this.loading.set(true); this.error.set(''); this.failedPage = page;
    this.request = this.http.get<{items: Item[]; totalCount: number}>(`${this.api}/lookups/${this.kind()}`, {
      params: {page, pageSize: 25, search: this.appliedSearch}
    }).subscribe({
      next: result => {
        const activeId = this.options()[this.activeIndex()]?.id;
        const ids = new Set(this.items().map(item => item.id));
        const added = result.items.filter(item => { if (ids.has(item.id)) return false; ids.add(item.id); return true; });
        this.items.update(items => [...items, ...added]); this.total.set(result.totalCount); this.loadedPage = page;
        const current = result.items.find(item => item.id === this.value()); if (current) this.selected.set(current);
        if (activeId !== undefined) this.activeIndex.set(this.options().findIndex(item => item.id === activeId));
        // Stop on an empty/non-advancing page even if the total changed during paging.
        this.hasMore.set(added.length > 0 && page * 25 < result.totalCount);
        this.loading.set(false);
        if (this.advanceAfterLoad && added.length) { this.activeIndex.update(index => index + 1); this.revealActive(); }
        this.advanceAfterLoad = false;
      },
      error: error => { this.error.set(apiError(error)); this.loading.set(false); this.advanceAfterLoad = false; }
    });
  }
  keydown(event: KeyboardEvent): void {
    if (this.disabled() || event.isComposing) return;
    if (event.key === 'Escape' && this.opened()) { event.preventDefault(); event.stopPropagation(); this.close(); return; }
    // Let the browser move focus before focusOut closes the list (especially in Firefox).
    if (event.key === 'Tab') return;
    if (event.key === 'Enter') {
      event.preventDefault();
      if (!this.opened()) { this.open(); return; }
      const item = this.options()[this.activeIndex()]; if (item) this.choose(item); return;
    }
    if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key) || (['Home', 'End'].includes(event.key) && !this.opened())) return;
    event.preventDefault(); this.open();
    const last = this.options().length - 1; const current = this.activeIndex();
    if (event.key === 'ArrowDown' && current === last && last >= 0 && this.hasMore()) {
      if (!this.loading() && !this.error()) { this.advanceAfterLoad = true; this.loadMore(); }
      return;
    }
    this.advanceAfterLoad = false;
    this.activeIndex.set(event.key === 'Home' ? 0 : event.key === 'End' ? last
      : event.key === 'ArrowDown' ? Math.min(last, current + 1) : current < 0 ? last : Math.max(0, current - 1));
    this.revealActive();
  }
  private revealActive(): void {
    requestAnimationFrame(() => {
      const list = this.listElement()?.nativeElement; const option = list?.children[this.activeIndex()] as HTMLElement | undefined;
      if (!list || !option) return;
      const offset = option.getBoundingClientRect().top - list.getBoundingClientRect().top + list.scrollTop;
      if (offset < list.scrollTop) list.scrollTop = offset;
      else if (offset + option.offsetHeight > list.scrollTop + list.clientHeight) list.scrollTop = offset + option.offsetHeight - list.clientHeight;
    });
  }
}
