import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/services/auth.service';
import { CardComponent } from '../../shared/components';

@Component({
  selector: 'app-complete-profile-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  templateUrl: './complete-profile.page.html',
  styleUrls: ['./complete-profile.page.css'],
})
export class CompleteProfilePage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly fullName = signal('');
  readonly phone = signal('');
  readonly address = signal('');
  readonly error = signal('');
  readonly loading = signal(false);
  readonly profileLoading = signal(false);

  constructor() {
    this.loadProfile();
  }

  loadProfile(): void {
    this.profileLoading.set(true);
    this.authService
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (p) => {
          this.fullName.set(p.fullName ?? '');
          this.phone.set(p.phone ?? '');
          this.address.set(p.address ?? '');
          this.profileLoading.set(false);
        },
        error: () => this.profileLoading.set(false),
      });
  }

  onSubmit(): void {
    this.error.set('');
    const phoneVal = this.phone().trim();
    const addressVal = this.address().trim();
    if (!phoneVal) {
      this.error.set('يرجى إدخال رقم التليفون');
      return;
    }
    if (!addressVal) {
      this.error.set('يرجى إدخال العنوان');
      return;
    }
    this.loading.set(true);
    this.authService
      .updateProfile({ phone: phoneVal, address: addressVal })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res?.token) {
            this.authService.setUserAfterProfileCompletion(res.token);
          }
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'فشل حفظ البيانات');
          this.loading.set(false);
        },
      });
  }
}
