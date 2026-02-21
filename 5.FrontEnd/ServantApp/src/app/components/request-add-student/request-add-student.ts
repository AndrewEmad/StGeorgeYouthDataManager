import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { StudentAdditionRequestService, CreateStudentAdditionRequestDto } from '../../services/student-addition-request.service';

@Component({
  selector: 'app-request-add-student',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './request-add-student.html',
  styleUrls: ['./request-add-student.css']
})
export class RequestAddStudentComponent {
  dto: CreateStudentAdditionRequestDto = {
    fullName: '',
    phone: '',
    secondaryPhone: '',
    address: '',
    area: '',
    college: '',
    academicYear: '',
    confessionFather: '',
    notes: '',
    gender: 0
  };
  error = '';
  loading = false;

  readonly genderOptions = [
    { value: 0, label: 'ذكر' },
    { value: 1, label: 'أنثى' }
  ];

  constructor(
    private additionRequestService: StudentAdditionRequestService,
    private router: Router
  ) {}

  onSubmit() {
    this.error = '';
    if (!this.dto.fullName?.trim() || !this.dto.phone?.trim()) {
      this.error = 'الاسم ورقم الهاتف مطلوبان';
      return;
    }
    this.loading = true;
    const payload = {
      ...this.dto,
      fullName: this.dto.fullName.trim(),
      phone: this.dto.phone.trim(),
      secondaryPhone: this.dto.secondaryPhone?.trim() || undefined,
      address: this.dto.address?.trim() || undefined,
      area: this.dto.area?.trim() || undefined,
      college: this.dto.college?.trim() || undefined,
      academicYear: this.dto.academicYear?.trim() || undefined,
      confessionFather: this.dto.confessionFather?.trim() || undefined,
      notes: this.dto.notes?.trim() || undefined,
      birthDate: this.dto.birthDate || undefined
    };
    this.additionRequestService.create(payload).subscribe({
      next: () => this.router.navigate(['/students']),
      error: (err) => {
        this.error = err.error?.message || 'فشل إرسال الطلب';
        this.loading = false;
      }
    });
  }
}
