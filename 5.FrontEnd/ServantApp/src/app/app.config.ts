import {serviceYearInterceptor} from '@ydm/shared/core/service-year';
import { YDM_FIREBASE } from '@ydm/shared/core/push';
import { YDM_API,YDM_SESSION,YDM_AUTH } from '@ydm/shared/core/api';
import { environment } from '../environments/environment';
import { AuthService } from './core/services/auth.service';
import { ApplicationConfig, isDevMode, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideServiceWorker } from '@angular/service-worker';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    {provide:YDM_AUTH,useExisting:AuthService},
    {provide:YDM_FIREBASE,useValue:environment.firebase},
    {provide:YDM_API,useValue:environment.apiUrl},
    {provide:YDM_SESSION,useFactory:(auth:AuthService)=>()=>({userId:auth.currentUser()?.userId || '',role:auth.currentUser()?.role || '',privileged:['Admin','Manager','Priest'].includes(auth.currentUser()?.role || ''),root:'',app:'Servant',namespace:'ydm.servantapp'}),deps:[AuthService]},
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withHashLocation()),
    provideHttpClient(withInterceptors([authInterceptor,serviceYearInterceptor])),
    provideServiceWorker('ngsw-worker.js', { enabled: !isDevMode() }),
  ],
};
