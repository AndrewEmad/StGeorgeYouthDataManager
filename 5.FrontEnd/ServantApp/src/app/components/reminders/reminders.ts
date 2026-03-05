import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReminderService, ScheduleDto } from '../../services/reminder.service';
import { AuthService } from '../../core/services/auth.service';

const DAY_NAMES = ['الأحد', 'الإثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

@Component({
  selector: 'app-reminders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reminders.html',
  styleUrls: ['./reminders.css']
})
export class RemindersComponent implements OnInit {
  newTime = '09:00';
  newDays: number[] = [];
  allDaysChecked = false;
  permissionMessage = '';
  slots: ScheduleDto[] = [];
  loading = false;
  fcmRegistered = false;

  readonly dayNames = DAY_NAMES;
  readonly dayNumbers = [0, 1, 2, 3, 4, 5, 6];

  constructor(
    public reminderService: ReminderService,
    private authService: AuthService
  ) {}

  logout(): void {
    this.authService.logout();
  }

  ngOnInit(): void {
    this.loadSlots();
    this.registerFcmToken();
  }

  loadSlots(): void {
    this.loading = true;
    this.reminderService.loadSchedules().subscribe({
      next: schedules => {
        this.slots = schedules;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  get permissionGranted(): boolean {
    return this.reminderService.permission === 'granted';
  }

  get permissionDenied(): boolean {
    return this.reminderService.permission === 'denied';
  }

  daysLabel(slot: ScheduleDto): string {
    const dayIndices = slot.daysOfWeek.split(',').map((d: string) => parseInt(d.trim(), 10));
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

  addSlot(): void {
    const days = this.allDaysChecked || this.newDays.length === 7 ? [0, 1, 2, 3, 4, 5, 6] : [...this.newDays];
    if (days.length === 0) return;
    this.reminderService.createSchedule({ timeOfDay: this.newTime, daysOfWeek: days.join(',') }).subscribe(() => {
        this.newDays = [];
        this.allDaysChecked = false;
        this.loadSlots();
    });
  }

  removeSlot(id: string): void {
    this.reminderService.deleteSchedule(id).subscribe(() => this.loadSlots());
  }

  async requestPermission(): Promise<void> {
    this.permissionMessage = '';
    const result = await this.reminderService.requestPermission();
    if (result === 'granted') this.permissionMessage = 'تم تفعيل التذكيرات';
    else if (result === 'denied') this.permissionMessage = 'تم رفض الإشعارات.';
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
