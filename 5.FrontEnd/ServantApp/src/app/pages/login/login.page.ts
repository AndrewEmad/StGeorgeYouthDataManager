import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ReminderService } from '../../services/reminder.service';
import { CardComponent } from '../../components/common/common';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent],
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.css']
})
export class LoginPage {
  username = '';
  password = '';
  error = '';
  loading = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private reminderService: ReminderService
  ) {}

  onSubmit() {
    if (!this.username || !this.password) return;
    this.loading = true;
    this.error = '';
    this.authService.login({ username: this.username, password: this.password }).subscribe({
      next: (res) => {
        this.reminderService.startChecking();
        this.router.navigate(res.requiresPasswordChange ? ['/change-password'] : ['/dashboard']);
      },
      error: () => {
        this.error = 'اسم المستخدم أو كلمة المرور غير صحيحة';
        this.loading = false;
      }
    });
  }
}
