import { Pipe, PipeTransform } from '@angular/core';
import { CALL_STATUS_OPTIONS } from '../models/follow-up.model';

@Pipe({ name: 'callStatusLabel', standalone: true })
export class CallStatusLabelPipe implements PipeTransform {
  transform(status: number): string {
    return CALL_STATUS_OPTIONS.find((o) => o.value === status)?.label ?? String(status);
  }
}
