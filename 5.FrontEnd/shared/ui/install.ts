import {Component,DestroyRef,inject,signal} from '@angular/core';
import {ModalComponent} from './modal';
interface InstallPrompt extends Event {prompt():Promise<void>;userChoice:Promise<{outcome:string}>;}
@Component({selector:'app-add-to-home-screen',standalone:true,imports:[ModalComponent],template:`
@if(available()){<footer class="ydm-install" dir="rtl"><button type="button" class="btn-outline" (click)="install()" [disabled]="busy()">تثبيت التطبيق</button></footer>}
<app-modal [visible]="instructions()" title="إضافة التطبيق إلى جهازك" [busy]="busy()" (close)="instructions.set(false)"><div body><p>{{help}}</p><p class="text-muted">يبقى إرسال البيانات بحاجة إلى اتصال. احفظ عملك قبل تحديث التطبيق.</p>@if(error()){<p class="text-error" role="alert">{{error()}}</p>}</div><div actions><button type="button" class="btn-primary" (click)="instructions.set(false)">تم</button></div></app-modal>`})
export class InstallComponent {
 available=signal(false);instructions=signal(false);busy=signal(false);error=signal('');private prompt:InstallPrompt|null=null;
 help=/iPad|iPhone|iPod/.test(navigator.userAgent) ? 'افتح قائمة المشاركة ثم اختر «إضافة إلى الشاشة الرئيسية».' : 'افتح قائمة المتصفح ثم اختر «تثبيت التطبيق» أو «إضافة إلى الشاشة الرئيسية» عند توفر الخيار.';
 constructor(){this.available.set(!matchMedia('(display-mode: standalone)').matches && !(navigator as Navigator & {standalone?:boolean}).standalone);const capture=(event:Event)=>{event.preventDefault();this.prompt=event as InstallPrompt;this.available.set(true);};const installed=()=>this.available.set(false);window.addEventListener('beforeinstallprompt',capture);window.addEventListener('appinstalled',installed);inject(DestroyRef).onDestroy(()=>{window.removeEventListener('beforeinstallprompt',capture);window.removeEventListener('appinstalled',installed);});}
 async install(){if(this.busy())return;if(!this.prompt){this.instructions.set(true);return;}this.busy.set(true);try{await this.prompt.prompt();const choice=await this.prompt.userChoice;if(choice.outcome==='accepted')this.available.set(false);}catch{this.error.set('تعذر فتح طلب التثبيت. استخدم قائمة المتصفح.');this.instructions.set(true);}finally{this.prompt=null;this.busy.set(false);}}
}
