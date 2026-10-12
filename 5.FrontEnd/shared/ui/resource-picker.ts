import { Component,DestroyRef,inject,input,output,signal,effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Subject,Subscription,debounceTime,distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { YDM_API,apiError } from '../core/api';
export interface Item {id:string;name:string;}
@Component({selector:'ydm-resource-picker',standalone:true,imports:[FormsModule],template:`
  <div class="ydm-picker"><label>{{label()}}<input class="form-input" type="search" [attr.aria-label]="'بحث ' + label()" [disabled]="disabled()" [ngModel]="search" (ngModelChange)="searchChanged($event)" placeholder="بحث بالاسم" [ngModelOptions]="{standalone:true}" /></label>
    <select class="form-input" [attr.aria-label]="label()" [disabled]="disabled() || loading()" [ngModel]="value() || ''" (ngModelChange)="choose($event)" [ngModelOptions]="{standalone:true}"><option value="">{{emptyLabel() || (allowEmpty() ? 'غير مسند' : 'اختر من القائمة')}}</option>
      @if(value() && !inPage()){<option [value]="value()">{{selectedName() || selectedLabel() || 'الاختيار الحالي'}}</option>}
      @for(item of items();track item.id){<option [value]="item.id">{{item.name}}</option>}
    </select>
    @if(loading()){<small role="status">جارٍ تحميل الخيارات…</small>}@if(error()){<small class="text-error" role="alert">{{error()}}</small><button class="btn-outline" type="button" (click)="load()" [disabled]="disabled() || loading()">إعادة المحاولة</button>}
    <div class="ydm-toolbar"><button class="btn-outline" type="button" [disabled]="page<=1 || disabled() || loading()" (click)="changePage(-1)">السابق</button><small>صفحة {{page}} · {{total()}} خيار</small><button class="btn-outline" type="button" [disabled]="page*25>=total() || disabled() || loading()" (click)="changePage(1)">التالي</button></div>
  </div>`})
export class ResourcePickerComponent {
  kind=input.required<string>();label=input.required<string>();value=input<string|null>(null);disabled=input(false);allowEmpty=input(false);emptyLabel=input('');valueChange=output<string|null>();selectedLabel=input('');itemChange=output<Item|null>();
  private api=inject(YDM_API);private http=inject(HttpClient);private destroy=inject(DestroyRef);private request?:Subscription;private terms=new Subject<string>();
  search='';page=1;items=signal<Item[]>([]);total=signal(0);loading=signal(false);error=signal('');selectedName=signal('');
  constructor(){effect(()=>{this.kind();this.page=1;this.load();});this.terms.pipe(debounceTime(300),distinctUntilChanged(),takeUntilDestroyed(this.destroy)).subscribe(()=>{this.page=1;this.load();});this.destroy.onDestroy(()=>this.request?.unsubscribe());}
  inPage(){return this.items().some(i=>i.id===this.value());}
  choose(id:string){this.selectedName.set(this.items().find(i=>i.id===id)?.name || '');this.valueChange.emit(id || null);this.itemChange.emit(this.items().find(i=>i.id===id) || null);}
  searchChanged(value:string){this.search=value;this.terms.next(value);}
  changePage(delta:number){this.page+=delta;this.load();}
  load(){this.request?.unsubscribe();this.loading.set(true);this.error.set('');this.request=this.http.get<{items:Item[];totalCount:number}>(`${this.api}/lookups/${this.kind()}`,{params:{page:this.page,pageSize:25,search:this.search}}).subscribe({next:r=>{this.items.set(r.items);this.total.set(r.totalCount);this.loading.set(false);},error:e=>{this.items.set([]);this.error.set(apiError(e));this.loading.set(false);}});}
}
