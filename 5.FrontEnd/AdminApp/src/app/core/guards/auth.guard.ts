import {inject} from '@angular/core';
import {Router,ActivatedRouteSnapshot,RouterStateSnapshot} from '@angular/router';
import {AuthService} from '../services/auth.service';
export const authGuard=(_:ActivatedRouteSnapshot,state:RouterStateSnapshot)=>{
 const auth=inject(AuthService),router=inject(Router),path=state.url.split('?')[0].replace(/\/$/,'');
 if(!auth.isLoggedIn())return router.parseUrl('/login');
 const destination=auth.requiresPasswordChange() ? '/change-password' : auth.requiresProfileCompletion() ? '/complete-profile' : '/dashboard';
 if((auth.requiresPasswordChange() && path!=='/change-password') || (!auth.requiresPasswordChange() && auth.requiresProfileCompletion() && path!=='/complete-profile') || (!auth.requiresPasswordChange() && path==='/change-password') || (!auth.requiresProfileCompletion() && path==='/complete-profile'))return router.parseUrl(destination);
 return true;
};
