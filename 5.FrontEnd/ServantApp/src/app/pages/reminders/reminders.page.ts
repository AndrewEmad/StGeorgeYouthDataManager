import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReminderService } from '../../services/reminder.service';
import type { ScheduleDto } from '../../shared/models/reminder.model';
import { CardComponent } from '../../shared/components';

const DAY_NAMES = ['الأحد', 'الإثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

@Component({
  selector: 'app-reminders-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  templateUrl: './reminders.page.html',
  styleUrls: ['./reminders.page.css'],
})
export class RemindersPage {
  readonly reminderService = inject(ReminderService);
  private readonly destroyRef = inject(DestroyRef);

  readonly newTime = signal('09:00');
  readonly newDays = signal<number[]>([]);
  readonly allDaysChecked = signal(false);
  readonly permissionMessage = signal('');
  readonly schedules = signal<ScheduleDto[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly fcmRegistered = signal(false);

  readonly dayNames = DAY_NAMES;
  readonly dayNumbers = [0, 1, 2, 3, 4, 5, 6];

  readonly permissionGranted = computed(() => this.reminderService.permission === 'granted');
  readonly permissionDenied = computed(() => this.reminderService.permission === 'denied');

  constructor() {
    this.loadSchedules();
    this.registerFcmToken();
  }

  loadSchedules(): void {
    this.loading.set(true);
    this.reminderService
      .loadSchedules()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (schedules) => {
          this.schedules.set(schedules);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  daysLabel(schedule: ScheduleDto): string {
    const dayIndices = schedule.daysOfWeek.split(',').map((d: string) => parseInt(d.trim(), 10));
    if (dayIndices.length === 7) return 'كل الأيام';
    return dayIndices.map((d: number) => DAY_NAMES[d]).join('، ');
  }

  toggleAllDays(): void {
    const checked = !this.allDaysChecked();
    this.allDaysChecked.set(checked);
    this.newDays.set(checked ? [0, 1, 2, 3, 4, 5, 6] : []);
  }

  toggleDay(day: number): void {
    const days = this.newDays();
    const i = days.indexOf(day);
    const next = i === -1 ? [...days, day].sort((a, b) => a - b) : days.filter((d) => d !== day);
    this.newDays.set(next);
    this.allDaysChecked.set(next.length === 7);
  }

  isDaySelected(day: number): boolean {
    return this.newDays().includes(day);
  }

  addSchedule(): void {
    const days = this.allDaysChecked() || this.newDays().length === 7 ? [0, 1, 2, 3, 4, 5, 6] : [...this.newDays()];
    if (days.length === 0) return;

    this.saving.set(true);
    this.reminderService
      .createSchedule({
        daysOfWeek: days.join(','),
        timeOfDay: this.newTime(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.newDays.set([]);
          this.allDaysChecked.set(false);
          this.saving.set(false);
          this.loadSchedules();
        },
        error: () => this.saving.set(false),
      });
  }

  removeSchedule(id: string): void {
    this.reminderService
      .deleteSchedule(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: () => this.loadSchedules() });
  }

  async requestPermission(): Promise<void> {
    this.permissionMessage.set('');
    const token = await this.reminderService.requestNotificationPermissionAndGetToken();
    if (token) {
      this.permissionMessage.set('تم تفعيل التذكيرات');
      this.reminderService.registerDeviceToken(token).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
      this.fcmRegistered.set(true);
    } else if (this.permissionDenied()) {
      this.permissionMessage.set('تم رفض الإشعارات. يمكنك تفعيلها من إعدادات المتصفح.');
    }
  }

  private async registerFcmToken(): Promise<void> {
    if (this.reminderService.permission !== 'granted') return;
    const token = await this.reminderService.requestNotificationPermissionAndGetToken();
    if (token) {
      this.reminderService.registerDeviceToken(token).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
      this.fcmRegistered.set(true);
    }
  }
}
