import { Component, ElementRef, afterNextRender, effect, inject, input, output, signal, viewChild, DestroyRef } from '@angular/core';

let nextDialogId = 0;
let lastTrigger: HTMLElement | null = null;
// WebKit does not focus buttons on pointer activation. Remember the real trigger.
if (typeof document !== 'undefined') document.addEventListener('pointerdown', event => {
  const target = event.target instanceof Element ? event.target.closest<HTMLElement>('button,a[href],input,select,textarea,[role="button"]') : null;
  if (target && !target.closest('dialog')) lastTrigger = target;
}, true);

@Component({
  selector: 'app-modal',
  standalone: true,
  template: `
    <dialog #dialog class="ydm-dialog" [class.ydm-dialog-drawer]="variant()==='drawer'" [attr.aria-labelledby]="headingId"
      (keydown)="onKeydown($event)" (cancel)="onCancel($event)" (click)="onBackdrop($event)">
      <div class="ydm-dialog-content">
        <header class="ydm-dialog-heading">
          <h2 [id]="headingId">{{ title() }}</h2>
          <button type="button" class="ydm-dialog-close ydm-icon-button" aria-label="إغلاق" [disabled]="busy()" (click)="close.emit()"><span class="material-icons" aria-hidden="true">close</span></button>
        </header>
        <div class="ydm-dialog-body"><ng-content select="[body]" /></div>
        <footer class="ydm-dialog-actions"><ng-content select="[actions]" /></footer>
      </div>
    </dialog>`,
})
export class ModalComponent {
  title = input.required<string>();
  visible = input.required<boolean>();
  busy = input(false);
  variant = input<'dialog'|'drawer'>('dialog');
  close = output<void>();
  readonly headingId = `ydm-dialog-${++nextDialogId}`;
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly mounted = signal(false);
  private previousFocus: HTMLElement | null = null;

  constructor() {
    const destroy = inject(DestroyRef);
    afterNextRender(() => {
      // One shared implementation, hosted at the document root and in the native top layer.
      document.body.append(this.dialog().nativeElement);
      this.mounted.set(true);
    });
    effect(() => {
      if (!this.mounted()) return;
      const dialog = this.dialog().nativeElement;
      if (this.visible() && !dialog.open) {
        this.previousFocus = document.activeElement instanceof HTMLElement && document.activeElement !== document.body ? document.activeElement : lastTrigger;
        dialog.showModal();
      } else if (!this.visible() && dialog.open) {
        dialog.close();
        this.restoreFocus();
      }
    });
    destroy.onDestroy(() => {
      if (this.mounted()) { this.dialog().nativeElement.close(); this.dialog().nativeElement.remove(); }
      this.restoreFocus();
    });
  }
  private restoreFocus(): void {
    const target = this.previousFocus;
    if (!target?.isConnected) return;
    target.focus({ preventScroll: true });
    requestAnimationFrame(() => { if (target.isConnected && !document.querySelector('dialog[open]')) target.focus({ preventScroll: true }); });
  }
  onKeydown(event: KeyboardEvent): void {
    if(event.key!=='Tab')return;
    const dialog=this.dialog().nativeElement;
    const nodes=Array.from(dialog.querySelectorAll<HTMLElement>('button:not(:disabled),a[href],input:not(:disabled),select:not(:disabled),textarea:not(:disabled),[tabindex]:not([tabindex="-1"])')).filter(e=>e.getClientRects().length>0 && !e.closest('fieldset:disabled'));
    const first=nodes[0],last=nodes[nodes.length-1];
    if(!first){event.preventDefault();dialog.focus();return;}
    if(event.shiftKey && (document.activeElement===first || document.activeElement===dialog)){event.preventDefault();last.focus();}
    else if(!event.shiftKey && (document.activeElement===last || document.activeElement===dialog)){event.preventDefault();first.focus();}
  }
  onCancel(event: Event): void { event.preventDefault(); if (!this.busy()) this.close.emit(); }
  onBackdrop(event: MouseEvent): void { if (event.target === this.dialog().nativeElement && !this.busy()) this.close.emit(); }
}
