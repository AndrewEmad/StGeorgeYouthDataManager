import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = (_: any, state: any) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const url = state?.url ?? router.url;

  if (!authService.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }
  if (authService.requiresPasswordChange() && !url.includes('change-password')) {
    router.navigate(['/change-password']);
    return false;
  }
  if (!authService.requiresPasswordChange() && url.includes('change-password')) {
    router.navigate(['/dashboard']);
    return false;
  }
  return true;
};
