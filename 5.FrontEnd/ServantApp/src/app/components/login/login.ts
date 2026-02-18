import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent {
  username = '';
  password = '';
  error = '';
  loading = false;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit() {
    if (!this.username || !this.password) return;
    
    this.loading = true;
    this.error = '';
    
    this.authService.login({ username: this.username, password: this.password }).subscribe({
      next: (res) => {
        this.router.navigate(res.requiresPasswordChange ? ['/change-password'] : ['/dashboard']);
      },
      error: (err) => {
        this.error = 'اسم المستخدم أو كلمة المرور غير صحيحة';
        this.loading = false;
      }
    });
  }
}
