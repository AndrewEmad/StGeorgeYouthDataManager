import { Component } from '@angular/core';
import { StudentListComponent } from '../../components/student-list/student-list';

@Component({
  selector: 'app-students-page',
  standalone: true,
  imports: [StudentListComponent],
  template: '<app-student-list></app-student-list>'
})
export class StudentsPage {}
