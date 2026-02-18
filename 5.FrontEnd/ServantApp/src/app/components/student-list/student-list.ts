import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StudentQueriesService } from '../../services/student-queries.service';
import { AuthService } from '../../services/auth.service';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './student-list.html',
  styleUrls: ['./student-list.css']
})
export class StudentListComponent implements OnInit {
  students: any[] = [];
  filteredStudents: any[] = [];
  searchTerm = '';
  loading = true;

  constructor(
    private studentQueriesService: StudentQueriesService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadStudents();
  }

  loadStudents() {
    const userId = this.authService.currentUser()?.userId;
    if (!userId) {
      this.students = [];
      this.filteredStudents = [];
      this.loading = false;
      return;
    }
    this.studentQueriesService.getByServantId(userId).subscribe({
      next: (data: any) => {
        this.students = data || [];
        this.filteredStudents = [...this.students];
        this.loading = false;
      },
      error: () => {
        // Mock data for UI demonstration
        this.students = [
          { id: '1', fullName: 'أبانوب عادل', area: 'المعادي', college: 'هندسة', phone: '0123456789' },
          { id: '2', fullName: 'مينا سمير', area: 'حلوان', college: 'تجارة', phone: '0111111111' },
          { id: '3', fullName: 'بيشوي ناصر', area: 'المعادي', college: 'طب', phone: '0100000000' }
        ];
        this.filteredStudents = [...this.students];
        this.loading = false;
      }
    });
  }

  onSearch() {
    this.filteredStudents = this.students.filter(s => 
      s.fullName.includes(this.searchTerm) || 
      s.area.includes(this.searchTerm)
    );
  }
}
