import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { StudentQueriesService } from '../../services/student-queries.service';
import { StudentCommandsService } from '../../services/student-commands.service';
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
  assignedStudents: any[] = [];
  unassignedStudents: any[] = [];
  searchTerm = '';
  loading = true;
  assigningId: string | null = null;

  constructor(
    private studentQueriesService: StudentQueriesService,
    private studentCommandsService: StudentCommandsService,
    private authService: AuthService
  ) {}

  get currentUserId(): string | null {
    return this.authService.currentUser()?.userId ?? null;
  }

  get filteredAssigned(): any[] {
    if (!this.searchTerm.trim()) return this.assignedStudents;
    const t = this.searchTerm.trim().toLowerCase();
    return this.assignedStudents.filter(s =>
      (s.fullName && s.fullName.toLowerCase().includes(t)) ||
      (s.area && s.area.toLowerCase().includes(t))
    );
  }

  get filteredUnassigned(): any[] {
    if (!this.searchTerm.trim()) return this.unassignedStudents;
    const t = this.searchTerm.trim().toLowerCase();
    return this.unassignedStudents.filter(s =>
      (s.fullName && s.fullName.toLowerCase().includes(t)) ||
      (s.area && s.area.toLowerCase().includes(t))
    );
  }

  ngOnInit() {
    this.loadStudents();
  }

  loadStudents() {
    const userId = this.currentUserId;
    if (!userId) {
      this.assignedStudents = [];
      this.unassignedStudents = [];
      this.loading = false;
      return;
    }
    this.loading = true;
    forkJoin({
      assigned: this.studentQueriesService.getByServantId(userId),
      unassigned: this.studentQueriesService.getPaged({ page: 1, pageSize: 100, hasServant: false })
    }).subscribe({
      next: ({ assigned, unassigned }) => {
        this.assignedStudents = assigned || [];
        this.unassignedStudents = unassigned?.items || [];
        this.loading = false;
      },
      error: () => {
        this.assignedStudents = [];
        this.unassignedStudents = [];
        this.loading = false;
      }
    });
  }

  onSearch() {}

  assignToMe(student: any) {
    const userId = this.currentUserId;
    if (!userId || this.assigningId) return;
    this.assigningId = student.id;
    this.studentCommandsService.assignToServant(student.id, userId).subscribe({
      next: () => {
        this.unassignedStudents = this.unassignedStudents.filter(s => s.id !== student.id);
        this.assignedStudents = [...this.assignedStudents, { ...student, servantId: userId }];
        this.assigningId = null;
      },
      error: () => { this.assigningId = null; }
    });
  }
}
