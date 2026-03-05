import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/services/auth.service';
import type { ProfileDto } from '../../shared/models/auth.model';
import { LoaderComponent, CardComponent } from '../../shared/components';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LoaderComponent, CardComponent],
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.css'],
})
export class ProfilePage {
  readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly profile = signal<ProfileDto | null>(null);
  readonly fullName = signal('');
  readonly email = signal('');
  readonly phone = signal('');
  readonly profileLoading = signal(false);
  readonly profileSaving = signal(false);
  readonly profileError = signal('');

  readonly currentPassword = signal('');
  readonly newPassword = signal('');
  readonly confirmPassword = signal('');
  readonly passwordError = signal('');
  readonly passwordSaving = signal(false);

  constructor() {
    this.loadProfile();
  }

  loadProfile(): void {
    this.profileLoading.set(true);
    this.profileError.set('');
    this.authService
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (p) => {
          this.profile.set(p);
          this.fullName.set(p.fullName ?? '');
          this.email.set(p.email ?? '');
          this.phone.set(p.phone ?? '');
          this.profileLoading.set(false);
        },
        error: () => {
          this.profileError.set('فشل تحميل الملف الشخصي');
          this.profileLoading.set(false);
        },
      });
  }

  saveProfile(): void {
    this.profileError.set('');
    this.profileSaving.set(true);
    this.authService
      .updateProfile({
        email: this.email().trim() || undefined,
        phone: this.phone().trim() || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.profileSaving.set(false),
        error: (err) => {
          this.profileError.set(err.error?.message || 'فشل حفظ التعديلات');
          this.profileSaving.set(false);
        },
      });
  }

  changePassword(): void {
    this.passwordError.set('');
    const current = this.currentPassword();
    const newP = this.newPassword();
    const confirm = this.confirmPassword();
    if (!current || !newP || !confirm) {
      this.passwordError.set('يرجى تعبئة جميع الحقول');
      return;
    }
    if (newP !== confirm) {
      this.passwordError.set('كلمة المرور الجديدة وتأكيدها غير متطابقين');
      return;
    }
    if (newP.length < 6) {
      this.passwordError.set('كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل');
      return;
    }
    this.passwordSaving.set(true);
    this.authService
      .changePassword(current, newP)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.authService.setUserAfterPasswordChange(res);
          this.currentPassword.set('');
          this.newPassword.set('');
          this.confirmPassword.set('');
          this.passwordSaving.set(false);
        },
        error: (err) => {
          this.passwordError.set(err.error?.message || 'فشل تغيير كلمة المرور');
          this.passwordSaving.set(false);
        },
      });
  }
}
