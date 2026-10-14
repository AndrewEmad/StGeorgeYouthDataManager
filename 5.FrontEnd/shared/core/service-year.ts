import {DestroyRef,Injectable,effect,inject,signal,untracked} from '@angular/core';
import {HttpInterceptorFn} from '@angular/common/http';
import {Subject,finalize,takeUntil} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {YDM_API,YDM_SESSION} from './api';
import {serviceYearPeriod} from './utils';

@Injectable({providedIn:'root'})
export class ServiceYearState {
  private session=inject(YDM_SESSION);
  year=signal<number|null>(null);
  pendingWrites=signal(0);
  readonly changes=new Subject<void>();
  private key='';
  constructor(){
    effect(()=>{const s=this.session();const key=s.userId ? `${s.namespace}.service-year.${s.userId}` : '';
      if(key===this.key)return;this.key=key;untracked(()=>this.restore());
    });
    const storage=(e:StorageEvent)=>{if(e.key===this.key || e.key===null)this.restore();};
    window.addEventListener('storage',storage);
    inject(DestroyRef).onDestroy(()=>window.removeEventListener('storage',storage));
  }
  period(){return this.year()===null ? null : serviceYearPeriod(this.year()!);}
  private restore(){let value:null|number=null;try{const stored=this.key ? localStorage.getItem(this.key) : null;if(stored!==null && /^\d{4}$/.test(stored)){const n=Number(stored);if(n>=1900 && n<=9997)value=n;}}catch{}
    if(value!==this.year()){this.year.set(value);this.changes.next();}
  }
  select(value:string){if(this.pendingWrites())return;const year=value==='' ? null : Number(value);if(year!==null && (!Number.isInteger(year) || year<1900 || year>9997))return;
    if(this.year()===year)return;this.year.set(year);try{if(this.key){year===null ? localStorage.removeItem(this.key) : localStorage.setItem(this.key,String(year));}}catch{}
    this.changes.next();
  }
}
// Register within the component injection context. A scope change cancels old reads;
// pages refresh only their data, preserving open forms and unsaved input.
export function onServiceYearChange(refresh:()=>void){const state=inject(ServiceYearState);state.changes.pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe(refresh);return state;}
export const serviceYearInterceptor:HttpInterceptorFn=(req,next)=>{
  const api=new URL(inject(YDM_API),location.origin),target=new URL(req.url,location.origin);
  if(target.origin!==api.origin || !(target.pathname===api.pathname || target.pathname.startsWith(api.pathname.replace(/\/$/,'')+'/')))return next(req);
  const state=inject(ServiceYearState);
  if(req.method!=='GET'){
    state.pendingWrites.update(n=>n+1);return next(req).pipe(finalize(()=>state.pendingWrites.update(n=>Math.max(0,n-1))));
  }
  if(/\/(Auth|Profile|Lookups|Notification|Heartbeat|operations)(\/|$)/i.test(target.pathname) || /\/(photo|areas|academic-years|settings)$/.test(target.pathname))return next(req);
  const year=untracked(()=>state.year());req=req.clone({params:req.params.set('serviceYear',year===null ? 'all' : String(year))});
  const temporal=/\/(reports|activity-history|approvals|follow-up-tasks|FollowUp|Attendance|meetings)(\/|$)/i.test(target.pathname) || /\/(audit|history)$/.test(target.pathname);
  return temporal ? next(req).pipe(takeUntil(state.changes)) : next(req);
};
