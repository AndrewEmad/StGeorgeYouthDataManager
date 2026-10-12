import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const api = new URL(environment.apiUrl, location.origin);
  const target = new URL(req.url, location.origin);
  if (target.origin !== api.origin || !(target.pathname === api.pathname || target.pathname.startsWith(api.pathname.replace(/\/$/, '') + '/'))) return next(req);
  // Authenticated API data always uses the network; only the static shell is cached.
  req=req.clone({setHeaders:{'ngsw-bypass':'true'}});
  const match=req.headers.get('If-Match');if(match && !match.startsWith('\"'))req=req.clone({setHeaders:{'If-Match':'\"'+match+'\"'}});
  const auth = inject(AuthService);
  const publicEndpoint = /\/Auth\/(login|refresh-token|logout)$/.test(target.pathname);
  const send = (token: string | null) => next(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);
  if (publicEndpoint) return next(req);
  const original=auth.currentUser();
  const clearOriginal=()=>{if(auth.currentUser()?.refreshToken===original?.refreshToken){auth.clearSession();auth.navigateAfterAuthentication();}};
  const refreshAndSend=()=>auth.refresh().pipe(
    catchError(error=>{clearOriginal();return throwError(()=>error);}),
    switchMap(session=>send(session.token).pipe(catchError(error=>{
      if(error.status===401 && auth.getToken()===session.token){auth.clearSession();auth.navigateAfterAuthentication();}
      return throwError(()=>error);
    })))
  );
  if(auth.isLoggedIn() && auth.isTokenExpired())return refreshAndSend();
  return send(original?.token ?? null).pipe(catchError((error:HttpErrorResponse)=>{
    if(error.status!==401 || !auth.isLoggedIn() || auth.currentUser()?.refreshToken!==original?.refreshToken)return throwError(()=>error);
    return refreshAndSend();
  }));
};
