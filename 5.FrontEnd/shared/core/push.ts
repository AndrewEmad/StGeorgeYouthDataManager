import { Injectable,InjectionToken,inject,signal,effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { YDM_API,YDM_SESSION,apiError } from './api';
export const YDM_FIREBASE = new InjectionToken<Record<string,string>>('YDM Firebase');
@Injectable({providedIn:'root'})
export class PushService {
  private api=inject(YDM_API);private context=inject(YDM_SESSION);private config=inject(YDM_FIREBASE);private http=inject(HttpClient);
  permission=signal<NotificationPermission>(typeof Notification==='undefined' ? 'denied' : Notification.permission);busy=signal(false);registered=signal(false);message=signal('');foreground=signal('');private syncing?:Promise<string|null>;private unsubscribeMessage?:()=>void;private accountId='';
  constructor(){
    effect(()=>{const s=this.context();if(this.accountId!==s.userId){this.unsubscribeMessage?.();this.unsubscribeMessage=undefined;this.foreground.set('');this.accountId=s.userId;}this.registered.set(false);if(s.userId && this.permission()==='granted')void this.sync(false);});
    document.addEventListener('visibilitychange',()=>{if(document.visibilityState==='visible'){this.permission.set(typeof Notification==='undefined' ? 'denied' : Notification.permission);if(this.context().userId && this.permission()==='granted')void this.sync(false);}});
  }
  sync(prompt=true):Promise<string|null>{if(this.syncing)return this.syncing;this.syncing=this.register(prompt).finally(()=>this.syncing=undefined);return this.syncing;}
  private async register(prompt:boolean):Promise<string|null>{
    const account=this.context();if(!account.userId || typeof Notification==='undefined' || !('serviceWorker' in navigator))return null;
    this.busy.set(true);this.message.set('');
    try{
      const permission=prompt ? await Notification.requestPermission() : Notification.permission;this.permission.set(permission);
      if(permission!=='granted'){this.message.set(permission==='denied' ? 'الإشعارات محظورة. يمكنك السماح بها من إعدادات المتصفح.' : 'لم تُفعّل الإشعارات بعد.');return null;}
      const {initializeApp,getApps}=await import('firebase/app');const {getMessaging,getToken,onMessage,isSupported}=await import('firebase/messaging');
      if(!await isSupported()){this.message.set('هذا المتصفح لا يدعم إشعارات الويب.');return null;}
      const app=getApps().find(a=>a.name===account.app) || initializeApp(this.config,account.app);const messaging=getMessaging(app);
      const root=new URL('./',document.baseURI);const scope=new URL('push/',root).href;const registration=await navigator.serviceWorker.register(new URL('firebase-messaging-sw.js',root).href,{scope});
      const token=await getToken(messaging,{vapidKey:this.config['vapidKey'],serviceWorkerRegistration:registration});if(!token)throw new Error('No device token');
      if(this.context().userId!==account.userId)return null;
      const old=localStorage.getItem(account.namespace+'.fcm_token');
      await firstValueFrom(this.http.post(`${this.api}/Notification/device-token`,{token,appType:account.app,deviceName:navigator.userAgent.slice(0,200)}));
      if(old && old!==token)await firstValueFrom(this.http.delete(`${this.api}/Notification/device-token`,{body:{token:old}}));
      if(this.context().userId!==account.userId)return null;
      localStorage.setItem(account.namespace+'.fcm_token',token);this.registered.set(true);this.message.set('تم تسجيل هذا الجهاز للإشعارات.');
      this.unsubscribeMessage?.();this.unsubscribeMessage=onMessage(messaging,payload=>{if(this.context().userId===account.userId)this.foreground.set(payload.notification?.title || 'إشعار متابعة جديد');});
      return token;
    }catch(error:any){this.registered.set(false);this.message.set(error?.status!==undefined ? apiError(error) : 'تعذر تسجيل الإشعارات. يمكنك المحاولة مجددًا.');return null;}
    finally{this.busy.set(false);}
  }
}
