import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CardComponent } from '../../components/common/common';

@Component({
  selector: 'app-complete-profile-page',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent],
  templateUrl: './complete-profile.page.html',
  styleUrls: ['./complete-profile.page.css']
})
export class CompleteProfilePage implements OnInit {
  fullName = '';
  phone = '';
  address = '';
  error = '';
  loading = false;
  profileLoading = false;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.profileLoading = true;
    this.authService.getProfile().subscribe({
      next: (p) => {
        this.fullName = p.fullName ?? '';
        this.phone = p.phone ?? '';
        this.address = p.address ?? '';
        this.profileLoading = false;
      },
      error: () => {
        this.profileLoading = false;
      }
    });
  }

  onSubmit() {
    this.error = '';
    if (!this.phone?.trim()) {
      this.error = 'يرجى إدخال رقم التليفون';
      return;
    }
    if (!this.address?.trim()) {
      this.error = 'يرجى إدخال العنوان';
      return;
    }
    this.loading = true;
    this.authService.updateProfile({ phone: this.phone.trim(), address: this.address.trim() }).subscribe({
      next: (res) => {
        this.loading = false;
        if (res?.token) {
          this.authService.setUserAfterProfileCompletion(res.token);
          this.router.navigate(['/dashboard']);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'فشل حفظ البيانات';
        this.loading = false;
      }
    });
  }
}
