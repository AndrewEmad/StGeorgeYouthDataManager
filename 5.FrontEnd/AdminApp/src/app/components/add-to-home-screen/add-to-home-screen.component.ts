import { Component, OnInit, OnDestroy, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

declare global {
  interface WindowEventMap {
    beforeinstallprompt: BeforeInstallPromptEvent;
  }
}
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

@Component({
  selector: 'app-add-to-home-screen',
  standalone: true,
  imports: [],
  templateUrl: './add-to-home-screen.component.html',
  styleUrls: ['./add-to-home-screen.component.css']
})
export class AddToHomeScreenComponent implements OnInit, OnDestroy {
  show = false;
  showInstructions = false;
  private deferredPrompt: BeforeInstallPromptEvent | null = null;
  private boundHandler = (e: BeforeInstallPromptEvent) => this.handleInstallPrompt(e);

  constructor(@Inject(PLATFORM_ID) private platformId: object) {}

  ngOnInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.isStandalone()) return;
    this.show = true;
    window.addEventListener('beforeinstallprompt', this.boundHandler);
  }

  ngOnDestroy() {
    if (isPlatformBrowser(this.platformId)) {
      window.removeEventListener('beforeinstallprompt', this.boundHandler);
    }
  }

  private isStandalone(): boolean {
    const nav = (window as Window).navigator as Navigator & { standalone?: boolean };
    return !!(
      (window.matchMedia && window.matchMedia('(display-mode: standalone)').matches) ||
      (nav.standalone === true) ||
      (document.referrer && document.referrer.includes('android-app://'))
    );
  }

  private handleInstallPrompt(e: BeforeInstallPromptEvent) {
    e.preventDefault();
    this.deferredPrompt = e;
    this.show = true;
  }

  async install() {
    if (!this.deferredPrompt) {
      this.showInstructions = true;
      return;
    }
    this.deferredPrompt.prompt();
    const { outcome } = await this.deferredPrompt.userChoice;
    this.deferredPrompt = null;
    if (outcome === 'accepted') this.show = false;
  }

  getInstallInstructions(): string {
    if (typeof navigator === 'undefined') return '';
    const ua = navigator.userAgent;
    if (/iPad|iPhone|iPod/.test(ua)) {
      return 'لإضافة التطبيق: اضغط زر المشاركة ثم اختر «إضافة إلى الشاشة الرئيسية».';
    }
    if (/Android/.test(ua)) {
      return 'لإضافة التطبيق: افتح قائمة المتصفح (⋮) واختر «تثبيت التطبيق» أو «إضافة إلى الشاشة الرئيسية».';
    }
    return 'لإضافة التطبيق للشاشة الرئيسية، استخدم خيار الإضافة من قائمة المتصفح.';
  }
}
