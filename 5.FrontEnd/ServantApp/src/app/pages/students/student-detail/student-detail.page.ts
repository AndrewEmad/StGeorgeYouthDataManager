import { Component } from '@angular/core';
import { StudentDetailComponent } from '../../../components/student-detail/student-detail';

@Component({
  selector: 'app-student-detail-page',
  standalone: true,
  imports: [StudentDetailComponent],
  template: '<app-student-detail></app-student-detail>'
})
export class StudentDetailPage {}
