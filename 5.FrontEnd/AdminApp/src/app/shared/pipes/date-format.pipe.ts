import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateFormat',
  standalone: true,
})
export class DateFormatPipe implements PipeTransform {
  transform(value: string | null | undefined | unknown): string {
    if (value == null || value === '') return '—';
    const date = new Date(value as string);
    if (isNaN(date.getTime())) return '—';
    return date.toLocaleDateString('ar-EG', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }
}
