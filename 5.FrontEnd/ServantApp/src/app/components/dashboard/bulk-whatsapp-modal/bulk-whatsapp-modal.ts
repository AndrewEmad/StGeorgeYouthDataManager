import {Component,input,output} from '@angular/core';
import {MessagingComponent} from '@ydm/shared/ui/messaging';
@Component({selector:'app-bulk-whatsapp-modal',standalone:true,imports:[MessagingComponent],template:`<ydm-messaging mode="whatsapp" (close)="close.emit()"/>`})
export class BulkWhatsAppModalComponent {userId=input<string|undefined>();close=output<void>();}
