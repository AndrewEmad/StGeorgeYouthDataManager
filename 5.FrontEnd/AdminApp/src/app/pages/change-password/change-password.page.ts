import { Component, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CardComponent, FormFieldComponent } from '../../components/common/common';

@Component({
  selector: 'app-change-password-page',
  standalone: true,
  imports: [FormsModule, CardComponent, FormFieldComponent],
  templateUrl: './change-password.page.html',
  styleUrls: ['./change-password.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordPage {
  private authService = inject(AuthService);
  private router = inject(Router);

  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  error = signal('');
  loading = signal(false);

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
    this.authService.changePassword(current, newP).subscribe({
      next: (res) => {
        this.authService.setUserAfterPasswordChange(res);
        this.router.navigate(res.requiresProfileCompletion ? ['/complete-profile'] : ['/dashboard']);
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message || 'فشل تغيير كلمة المرور. تحقق من كلمة المرور الحالية.');
        this.loading.set(false);
      },
    });
  }
}
