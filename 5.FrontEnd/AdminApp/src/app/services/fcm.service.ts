import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, from } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FcmService {
  private apiUrl = `${environment.apiUrl}/Notification`;

  constructor(private http: HttpClient) {}

  registerDeviceToken(token: string, deviceName?: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/device-token`, { 
        token, 
        deviceName,
        appType: 'Admin' 
    });
  }

  unregisterDeviceToken(token: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/device-token`, { body: { token } });
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
          || await navigator.serviceWorker.register('/firebase-messaging-sw.js')
      });

      return token || null;
    } catch (err) {
      console.error('Failed to get FCM token:', err);
      return null;
    }
  }
}
