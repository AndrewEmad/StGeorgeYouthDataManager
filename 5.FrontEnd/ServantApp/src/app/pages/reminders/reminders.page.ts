import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReminderService, ScheduleDto } from '../../services/reminder.service';
import { AuthService } from '../../services/auth.service';
import { CardComponent } from '../../components/common/common';

const DAY_NAMES = ['الأحد', 'الإثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

@Component({
  selector: 'app-reminders-page',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent],
  templateUrl: './reminders.page.html',
  styleUrls: ['./reminders.page.css']
})
export class RemindersPage implements OnInit {
  newTime = '09:00';
  newDays: number[] = [];
  allDaysChecked = false;
  permissionMessage = '';
  schedules: ScheduleDto[] = [];
  loading = false;
  saving = false;
  fcmRegistered = false;

  readonly dayNames = DAY_NAMES;
  readonly dayNumbers = [0, 1, 2, 3, 4, 5, 6];

  constructor(
    public reminderService: ReminderService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadSchedules();
    this.registerFcmToken();
  }

  loadSchedules(): void {
    this.loading = true;
    this.reminderService.loadSchedules().subscribe({
      next: schedules => {
        this.schedules = schedules;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  get permissionGranted(): boolean {
    return this.reminderService.permission === 'granted';
  }

  get permissionDenied(): boolean {
    return this.reminderService.permission === 'denied';
  }

  daysLabel(schedule: ScheduleDto): string {
    const dayIndices = schedule.daysOfWeek.split(',').map((d: string) => parseInt(d.trim(), 10));
    if (dayIndices.length === 7) return 'كل الأيام';
    return dayIndices.map((d: number) => DAY_NAMES[d]).join('، ');
  }

  toggleAllDays(): void {
    this.allDaysChecked = !this.allDaysChecked;
    this.newDays = this.allDaysChecked ? [0, 1, 2, 3, 4, 5, 6] : [];
  }

  toggleDay(day: number): void {
    const i = this.newDays.indexOf(day);
    if (i === -1) this.newDays.push(day);
    else this.newDays.splice(i, 1);
    this.newDays.sort((a, b) => a - b);
    this.allDaysChecked = this.newDays.length === 7;
  }

  isDaySelected(day: number): boolean {
    return this.newDays.includes(day);
  }

  addSchedule(): void {
    const days = this.allDaysChecked || this.newDays.length === 7
      ? [0, 1, 2, 3, 4, 5, 6]
      : [...this.newDays];
    if (days.length === 0) return;

    this.saving = true;
    this.reminderService.createSchedule({
      daysOfWeek: days.join(','),
      timeOfDay: this.newTime
    }).subscribe({
      next: () => {
        this.newDays = [];
        this.allDaysChecked = false;
        this.saving = false;
        this.loadSchedules();
      },
      error: () => {
        this.saving = false;
      }
    });
  }

  removeSchedule(id: string): void {
    this.reminderService.deleteSchedule(id).subscribe({
      next: () => this.loadSchedules()
    });
  }

  async requestPermission(): Promise<void> {
    this.permissionMessage = '';
    const token = await this.reminderService.requestNotificationPermissionAndGetToken();
    if (token) {
      this.permissionMessage = 'تم تفعيل التذكيرات';
      this.reminderService.registerDeviceToken(token).subscribe();
      this.fcmRegistered = true;
    } else if (this.permissionDenied) {
      this.permissionMessage = 'تم رفض الإشعارات. يمكنك تفعيلها من إعدادات المتصفح.';
    }
  }

  private async registerFcmToken(): Promise<void> {
    if (this.reminderService.permission !== 'granted') return;
    const token = await this.reminderService.requestNotificationPermissionAndGetToken();
    if (token) {
      this.reminderService.registerDeviceToken(token).subscribe();
      this.fcmRegistered = true;
    }
  }
}
