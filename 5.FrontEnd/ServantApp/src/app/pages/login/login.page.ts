import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/services/auth.service';
import { CardComponent } from '../../shared/components';

@Component({
  selector: 'app-login-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.css'],
})
export class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly username = signal('');
  readonly password = signal('');
  readonly error = signal('');
  readonly loading = signal(false);

  onSubmit(): void {
    const u = this.username().trim();
    const p = this.password();
    if (!u || !p) return;
    this.loading.set(true);
    this.error.set('');
    this.authService
      .login({ username: u, password: p })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.router.navigate(res.requiresPasswordChange ? ['/change-password'] : ['/dashboard']);
        },
        error: () => {
          this.error.set('اسم المستخدم أو كلمة المرور غير صحيحة');
          this.loading.set(false);
        },
      });
  }
}
