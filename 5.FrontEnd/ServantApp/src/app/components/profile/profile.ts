import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService, ProfileDto } from '../../services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './profile.html',
  styleUrls: ['./profile.css']
})
export class ProfileComponent implements OnInit {
  profile: ProfileDto | null = null;
  fullName = '';
  email = '';
  phone = '';
  profileLoading = false;
  profileSaving = false;
  profileError = '';

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  passwordError = '';
  passwordSaving = false;

  constructor(public authService: AuthService) {}

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.profileLoading = true;
    this.profileError = '';
    this.authService.getProfile().subscribe({
      next: (p) => {
        this.profile = p;
        this.fullName = p.fullName ?? '';
        this.email = p.email ?? '';
        this.phone = p.phone ?? '';
        this.profileLoading = false;
      },
      error: () => {
        this.profileError = 'فشل تحميل الملف الشخصي';
        this.profileLoading = false;
      }
    });
  }

  saveProfile() {
    this.profileError = '';
    this.profileSaving = true;
    this.authService.updateProfile({ email: this.email.trim() || undefined, phone: this.phone.trim() || undefined }).subscribe({
      next: () => {
        this.profileSaving = false;
      },
      error: (err) => {
        this.profileError = err.error?.message || 'فشل حفظ التعديلات';
        this.profileSaving = false;
      }
    });
  }

  changePassword() {
    this.passwordError = '';
    if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
      this.passwordError = 'يرجى تعبئة جميع الحقول';
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError = 'كلمة المرور الجديدة وتأكيدها غير متطابقين';
      return;
    }
    if (this.newPassword.length < 6) {
      this.passwordError = 'كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل';
      return;
    }
    this.passwordSaving = true;
    this.authService.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: (res) => {
        this.authService.setUserAfterPasswordChange(res);
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.passwordSaving = false;
      },
      error: (err) => {
        this.passwordError = err.error?.message || 'فشل تغيير كلمة المرور';
        this.passwordSaving = false;
      }
    });
  }
}
