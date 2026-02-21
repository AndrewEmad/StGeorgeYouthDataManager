import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ReminderService, ReminderSlot } from '../../services/reminder.service';
import { AuthService } from '../../services/auth.service';

const DAY_NAMES = ['الأحد', 'الإثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

@Component({
  selector: 'app-reminders',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './reminders.html',
  styleUrls: ['./reminders.css']
})
export class RemindersComponent implements OnInit {
  newTime = '09:00';
  newDays: number[] = [];
  allDaysChecked = false;
  permissionMessage = '';
  slots: ReminderSlot[] = [];

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
  }

  loadSlots(): void {
    this.slots = this.reminderService.getSlots();
  }

  get permissionGranted(): boolean {
    return this.reminderService.permission === 'granted';
  }

  get permissionDenied(): boolean {
    return this.reminderService.permission === 'denied';
  }

  daysLabel(slot: ReminderSlot): string {
    const days = slot.days.length === 0 ? [0, 1, 2, 3, 4, 5, 6] : slot.days;
    if (days.length === 7) return 'كل الأيام';
    return days.map(d => DAY_NAMES[d]).join('، ');
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
    this.reminderService.addSlot({ time: this.newTime, days });
    this.newDays = [];
    this.allDaysChecked = false;
    this.loadSlots();
  }

  removeSlot(index: number): void {
    this.reminderService.removeSlot(index);
    this.loadSlots();
  }

  async requestPermission(): Promise<void> {
    this.permissionMessage = '';
    const result = await this.reminderService.requestPermission();
    if (result === 'granted') this.permissionMessage = 'تم تفعيل التذكيرات';
    else if (result === 'denied') this.permissionMessage = 'تم رفض الإشعارات. التذكيرات تعمل عند فتح التطبيق فقط عند السماح لاحقاً.';
  }
}
