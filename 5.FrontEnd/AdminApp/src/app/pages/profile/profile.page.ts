import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ProfileDto } from '../../shared/models';
import { ContentHeaderComponent, LoaderComponent, DetailSectionComponent, FormFieldComponent } from '../../components/common/common';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [FormsModule, ContentHeaderComponent, LoaderComponent, DetailSectionComponent, FormFieldComponent],
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage implements OnInit {
  private authService = inject(AuthService);

  profile = signal<ProfileDto | null>(null);
  fullName = signal('');
  email = signal('');
  phone = signal('');
  profileLoading = signal(false);
  profileSaving = signal(false);
  profileError = signal('');

  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  passwordError = signal('');
  passwordSaving = signal(false);

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.profileLoading.set(true);
    this.profileError.set('');
    this.authService.getProfile().subscribe({
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
        fullName: this.fullName().trim() || undefined,
        email: this.email().trim() || undefined,
        phone: this.phone().trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.authService.refreshCurrentUserFullName(this.fullName().trim());
          this.profileSaving.set(false);
        },
        error: (err: { error?: { message?: string } }) => {
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
    this.authService.changePassword(current, newP).subscribe({
      next: () => {
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
        this.passwordSaving.set(false);
      },
      error: (err: { error?: { message?: string } }) => {
        this.passwordError.set(err.error?.message || 'فشل تغيير كلمة المرور');
        this.passwordSaving.set(false);
      },
    });
  }
}
