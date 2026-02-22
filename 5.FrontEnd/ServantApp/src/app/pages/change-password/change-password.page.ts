import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CardComponent } from '../../components/common/common';

@Component({
  selector: 'app-change-password-page',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent],
  templateUrl: './change-password.page.html',
  styleUrls: ['./change-password.page.css']
})
export class ChangePasswordPage {
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  error = '';
  loading = false;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit() {
    this.error = '';
    if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
      this.error = 'يرجى تعبئة جميع الحقول';
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.error = 'كلمة المرور الجديدة وتأكيدها غير متطابقين';
      return;
    }
    if (this.newPassword.length < 6) {
      this.error = 'كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل';
      return;
    }
    this.loading = true;
    this.authService.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: (res) => {
        this.authService.setUserAfterPasswordChange(res);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.error = err.error?.message || 'فشل تغيير كلمة المرور. تحقق من كلمة المرور الحالية.';
        this.loading = false;
      }
    });
  }
}
