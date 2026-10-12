import {Component,input,output} from '@angular/core';
import {MessagingComponent} from '@ydm/shared/ui/messaging';
@Component({selector:'app-bulk-sms-modal',standalone:true,imports:[MessagingComponent],template:`<ydm-messaging mode="sms" (close)="close.emit()"/>`})
export class BulkSmsModalComponent {userId=input<string|undefined>();close=output<void>();}
