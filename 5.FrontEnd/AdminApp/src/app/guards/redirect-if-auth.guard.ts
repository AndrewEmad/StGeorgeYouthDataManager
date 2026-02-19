import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const redirectIfAuthGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.shouldRedirectToHome()) {
    if (authService.requiresPasswordChange()) {
      return router.parseUrl('/change-password');
    }
    return router.parseUrl('/dashboard');
  }
  return true;
};
