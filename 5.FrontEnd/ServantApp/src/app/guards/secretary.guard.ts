import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const secretaryGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isSecretary()) {
    return true;
  }
  router.navigate(['/dashboard']);
  return false;
};
