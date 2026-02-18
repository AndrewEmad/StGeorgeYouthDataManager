import { Component } from '@angular/core';
import { StudentListAdminComponent } from '../../components/students/student-list-admin/student-list-admin';

@Component({
  selector: 'app-students-page',
  standalone: true,
  imports: [StudentListAdminComponent],
  templateUrl: './students.page.html'
})
export class StudentsPage {}
