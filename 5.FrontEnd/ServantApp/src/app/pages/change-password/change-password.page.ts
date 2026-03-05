import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/services/auth.service';
import { CardComponent } from '../../shared/components';

@Component({
  selector: 'app-change-password-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  templateUrl: './change-password.page.html',
  styleUrls: ['./change-password.page.css'],
})
export class ChangePasswordPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly currentPassword = signal('');
  readonly newPassword = signal('');
  readonly confirmPassword = signal('');
  readonly error = signal('');
  readonly loading = signal(false);

  onSubmit(): void {
    this.error.set('');
    const current = this.currentPassword();
    const newP = this.newPassword();
    const confirm = this.confirmPassword();
    if (!current || !newP || !confirm) {
      this.error.set('يرجى تعبئة جميع الحقول');
      return;
    }
    if (newP !== confirm) {
      this.error.set('كلمة المرور الجديدة وتأكيدها غير متطابقين');
      return;
    }
    if (newP.length < 6) {
      this.error.set('كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل');
      return;
    }
    this.loading.set(true);
    this.authService
      .changePassword(current, newP)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.authService.setUserAfterPasswordChange(res);
          this.router.navigate(res.requiresProfileCompletion ? ['/complete-profile'] : ['/dashboard']);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'فشل تغيير كلمة المرور. تحقق من كلمة المرور الحالية.');
          this.loading.set(false);
        },
      });
  }
}
