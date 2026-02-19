import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { StudentQueriesService } from '../../services/student-queries.service';
import { AssignmentRequestService } from '../../services/assignment-request.service';
import { AuthService } from '../../services/auth.service';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

export interface UnassignedItem {
  student: any;
  hasPendingRequestByMe: boolean;
}

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './student-list.html',
  styleUrls: ['./student-list.css']
})
export class StudentListComponent implements OnInit {
  assignedStudents: any[] = [];
  unassignedForServantItems: UnassignedItem[] = [];
  searchTerm = '';
  loading = true;
  requestingId: string | null = null;

  constructor(
    private studentQueriesService: StudentQueriesService,
    private assignmentRequestService: AssignmentRequestService,
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

  get filteredUnassignedAvailable(): UnassignedItem[] {
    const items = this.unassignedForServantItems.filter(i => !i.hasPendingRequestByMe);
    if (!this.searchTerm.trim()) return items;
    const t = this.searchTerm.trim().toLowerCase();
    return items.filter(i =>
      (i.student?.fullName && i.student.fullName.toLowerCase().includes(t)) ||
      (i.student?.area && i.student.area.toLowerCase().includes(t))
    );
  }

  get filteredRequestedPending(): UnassignedItem[] {
    const items = this.unassignedForServantItems.filter(i => i.hasPendingRequestByMe);
    if (!this.searchTerm.trim()) return items;
    const t = this.searchTerm.trim().toLowerCase();
    return items.filter(i =>
      (i.student?.fullName && i.student.fullName.toLowerCase().includes(t)) ||
      (i.student?.area && i.student.area.toLowerCase().includes(t))
    );
  }

  ngOnInit() {
    this.loadStudents();
  }

  loadStudents() {
    const userId = this.currentUserId;
    if (!userId) {
      this.assignedStudents = [];
      this.unassignedForServantItems = [];
      this.loading = false;
      return;
    }
    this.loading = true;
    forkJoin({
      assigned: this.studentQueriesService.getByServantId(userId),
      unassigned: this.studentQueriesService.getUnassignedForServant({ page: 1, pageSize: 100 })
    }).subscribe({
      next: ({ assigned, unassigned }) => {
        this.assignedStudents = assigned || [];
        this.unassignedForServantItems = unassigned?.items || [];
        this.loading = false;
      },
      error: () => {
        this.assignedStudents = [];
        this.unassignedForServantItems = [];
        this.loading = false;
      }
    });
  }

  onSearch() {}

  requestAssignToMe(item: UnassignedItem) {
    const student = item.student;
    if (!student?.id || this.requestingId) return;
    this.requestingId = student.id;
    this.assignmentRequestService.create(student.id).subscribe({
      next: () => {
        this.unassignedForServantItems = this.unassignedForServantItems.map(i =>
          i.student?.id === student.id ? { ...i, hasPendingRequestByMe: true } : i
        );
        this.requestingId = null;
      },
      error: () => { this.requestingId = null; }
    });
  }
}
