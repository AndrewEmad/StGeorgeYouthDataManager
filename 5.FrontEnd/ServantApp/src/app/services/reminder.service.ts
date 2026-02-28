import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap, map, catchError, of } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ScheduleDto {
  id: string;
  daysOfWeek: string;
  timeOfDay: string;
  isActive: boolean;
}

export interface CreateScheduleRequest {
  daysOfWeek: string;
  timeOfDay: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReminderService {
  private apiUrl = `${environment.apiUrl}/Notification`;
  private _schedules$ = new BehaviorSubject<ScheduleDto[]>([]);
  schedules$ = this._schedules$.asObservable();

  constructor(private http: HttpClient) {}

  loadSchedules(): Observable<ScheduleDto[]> {
    return this.http.get<ScheduleDto[]>(`${this.apiUrl}/schedules`).pipe(
      tap(schedules => this._schedules$.next(schedules)),
      catchError(() => {
        this._schedules$.next([]);
        return of([]);
      })
    );
  }

  createSchedule(request: CreateScheduleRequest): Observable<ScheduleDto> {
    return this.http.post<ScheduleDto>(`${this.apiUrl}/schedules`, request).pipe(
      tap(() => this.loadSchedules().subscribe())
    );
  }

  deleteSchedule(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/schedules/${id}`).pipe(
      tap(() => this.loadSchedules().subscribe())
    );
  }

  registerDeviceToken(token: string, deviceName?: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/device-token`, { token, deviceName });
  }

  unregisterDeviceToken(token: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/device-token`, { body: { token } });
  }

  async requestPermission(): Promise<NotificationPermission> {
    await this.requestNotificationPermissionAndGetToken();
    return this.permission;
  }

  async requestNotificationPermissionAndGetToken(): Promise<string | null> {
    try {
      if (typeof Notification === 'undefined') return null;

      const permission = await Notification.requestPermission();
      if (permission !== 'granted') return null;

      // Dynamically import firebase to keep bundle size small
      const { initializeApp, getApps } = await import('firebase/app');
      const { getMessaging, getToken } = await import('firebase/messaging');

      if (getApps().length === 0) {
        initializeApp(environment.firebase);
      }

      const messaging = getMessaging();
      const token = await getToken(messaging, {
        vapidKey: (environment.firebase as any).vapidKey,
        serviceWorkerRegistration: await navigator.serviceWorker.getRegistration('/firebase-messaging-sw.js')
          || await navigator.serviceWorker.register('/StGeorgeYouthDataManager/servant/firebase-messaging-sw.js')
      });

      console.log('Firebase Cloud Messaging Token:', token);

      return token || null;
    } catch (err) {
      console.error('Failed to get FCM token:', err);
      return null;
    }
  }

  get permission(): NotificationPermission {
    if (typeof Notification === 'undefined') return 'denied';
    return Notification.permission;
  }
}
