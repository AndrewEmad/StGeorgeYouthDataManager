import { Pipe, PipeTransform } from '@angular/core';

const CALL_STATUS_MAP: Record<number, string> = {
  0: 'لم يرد',
  1: 'رد',
  2: 'مشغول',
  3: 'مغلق',
  4: 'رقم خاطئ',
};

@Pipe({
  name: 'callStatus',
  standalone: true,
})
export class CallStatusPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null) return '—';
    return CALL_STATUS_MAP[value] ?? String(value);
  }
}
