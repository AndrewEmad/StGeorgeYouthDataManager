import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = (_: unknown, state: unknown) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const url = (state as { url?: string })?.url ?? router.url;

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
  if (!authService.requiresPasswordChange()) {
    if (authService.requiresProfileCompletion() && !url.includes('complete-profile')) {
      router.navigate(['/complete-profile']);
      return false;
    }
    if (!authService.requiresProfileCompletion() && url.includes('complete-profile')) {
      router.navigate(['/dashboard']);
      return false;
    }
  }
  return true;
};
