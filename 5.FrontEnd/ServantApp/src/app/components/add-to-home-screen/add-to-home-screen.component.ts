import { Component, OnInit, OnDestroy, PLATFORM_ID, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  imports: [CommonModule],
  templateUrl: './add-to-home-screen.component.html',
  styleUrls: ['./add-to-home-screen.component.css']
})
export class AddToHomeScreenComponent implements OnInit, OnDestroy {
  show = false;
  private deferredPrompt: BeforeInstallPromptEvent | null = null;
  private boundHandler = (e: BeforeInstallPromptEvent) => this.handleInstallPrompt(e);

  constructor(@Inject(PLATFORM_ID) private platformId: object) {}

  ngOnInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.isStandalone()) return;
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
    if (!this.deferredPrompt) return;
    this.deferredPrompt.prompt();
    const { outcome } = await this.deferredPrompt.userChoice;
    this.deferredPrompt = null;
    this.show = false;
  }

  dismiss() {
    this.show = false;
  }
}
