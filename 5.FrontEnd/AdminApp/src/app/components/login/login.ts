import { Component, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CardComponent, FormFieldComponent } from '../common/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CardComponent, FormFieldComponent],
  templateUrl: './login.html',
  styleUrl: './login.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  username = signal('');
  password = signal('');
  error = signal('');
  loading = signal(false);

  onSubmit(): void {
    const u = this.username();
    const p = this.password();
    if (!u || !p) return;

    this.loading.set(true);
    this.error.set('');

    this.authService.login({ username: u, password: p }).subscribe({
      next: (res) => {
        const target = res.requiresPasswordChange ? '/change-password' : res.requiresProfileCompletion ? '/complete-profile' : '/dashboard';
        this.router.navigate([target]);
      },
      error: () => {
        this.error.set('اسم المستخدم أو كلمة المرور غير صحيحة');
        this.loading.set(false);
      },
    });
  }
}
