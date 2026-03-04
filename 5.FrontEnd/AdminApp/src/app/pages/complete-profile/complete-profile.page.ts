import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CardComponent, FormFieldComponent } from '../../components/common/common';

@Component({
  selector: 'app-complete-profile-page',
  standalone: true,
  imports: [FormsModule, CardComponent, FormFieldComponent],
  templateUrl: './complete-profile.page.html',
  styleUrls: ['./complete-profile.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompleteProfilePage implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  fullName = signal('');
  phone = signal('');
  address = signal('');
  error = signal('');
  loading = signal(false);
  profileLoading = signal(false);

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.profileLoading.set(true);
    this.authService.getProfile().subscribe({
      next: (p) => {
        this.fullName.set(p.fullName ?? '');
        this.phone.set(p.phone ?? '');
        this.address.set((p as { address?: string }).address ?? '');
        this.profileLoading.set(false);
      },
      error: () => {
        this.profileLoading.set(false);
      },
    });
  }

  onSubmit(): void {
    this.error.set('');
    const fullName = this.fullName().trim();
    const phone = this.phone().trim();
    const address = this.address().trim();
    if (!fullName) {
      this.error.set('يرجى إدخال الاسم الكامل');
      return;
    }
    if (!phone) {
      this.error.set('يرجى إدخال رقم التليفون');
      return;
    }
    if (!address) {
      this.error.set('يرجى إدخال العنوان');
      return;
    }
    this.loading.set(true);
    this.authService.updateProfile({ fullName, phone, address }).subscribe({
      next: (res: { token?: string }) => {
        this.loading.set(false);
        if (res?.token) {
          this.authService.setUserAfterProfileCompletion(res.token);
          this.authService.refreshCurrentUserFullName(fullName);
        }
        this.router.navigate(['/dashboard']);
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message || 'فشل حفظ البيانات');
        this.loading.set(false);
      },
    });
  }
}
