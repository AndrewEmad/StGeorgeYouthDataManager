import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';

const STORAGE_KEY = 'youth_servant_reminders';

export interface ReminderSlot {
  time: string;
  days: number[];
}

@Injectable({
  providedIn: 'root'
})
export class ReminderService {
  private intervalId: ReturnType<typeof setInterval> | null = null;
  private lastShownForDate: Map<string, string> = new Map();

  constructor(private authService: AuthService) {}

  getSlots(): ReminderSlot[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as ReminderSlot[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  setSlots(slots: ReminderSlot[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(slots));
  }

  addSlot(slot: ReminderSlot): void {
    const slots = this.getSlots();
    slots.push(slot);
    this.setSlots(slots);
  }

  removeSlot(index: number): void {
    const slots = this.getSlots();
    if (index < 0 || index >= slots.length) return;
    slots.splice(index, 1);
    this.setSlots(slots);
  }

  async requestPermission(): Promise<NotificationPermission> {
    if (typeof Notification === 'undefined') return 'denied';
    return Notification.requestPermission();
  }

  get permission(): NotificationPermission {
    if (typeof Notification === 'undefined') return 'denied';
    return Notification.permission;
  }

  startChecking(): void {
    if (this.intervalId != null) return;
    this.intervalId = setInterval(() => this.check(), 60_000);
    this.check();
  }

  private check(): void {
    if (!this.authService.currentUser()) {
      if (this.intervalId != null) {
        clearInterval(this.intervalId);
        this.intervalId = null;
      }
      return;
    }
    if (typeof Notification === 'undefined' || Notification.permission !== 'granted') return;

    const slots = this.getSlots();
    if (slots.length === 0) return;

    const now = new Date();
    const today = now.toISOString().slice(0, 10);
    const currentDay = now.getDay();
    const hours = now.getHours();
    const minutes = now.getMinutes();
    const nowTime = `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;

    for (let i = 0; i < slots.length; i++) {
      const slot = slots[i];
      const days = slot.days.length === 0 ? [0, 1, 2, 3, 4, 5, 6] : slot.days;
      if (!days.includes(currentDay) || slot.time !== nowTime) continue;

      const slotKey = `${i}-${slot.time}-${slot.days.join(',')}`;
      if (this.lastShownForDate.get(slotKey) === today) continue;

      try {
        new Notification('تذكير متابعة', { body: 'وقت متابعة المخدومين' });
        this.lastShownForDate.set(slotKey, today);
      } catch (_) {}
    }
  }
}
