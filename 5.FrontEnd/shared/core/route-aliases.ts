import {inject} from '@angular/core';
import {RedirectFunction,Router} from '@angular/router';

export function requestsRedirect(root:string,defaults:Record<string,string>):RedirectFunction {
  return ({queryParams})=>inject(Router).createUrlTree([root+'/requests'],{queryParams:{...defaults,...(root==='/dashboard' && queryParams['requestId'] ? {mine:'false',status:''} : {}),...queryParams}});
}
