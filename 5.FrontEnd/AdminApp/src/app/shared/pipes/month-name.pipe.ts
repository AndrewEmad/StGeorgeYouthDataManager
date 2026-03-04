import { Pipe, PipeTransform } from '@angular/core';

const MONTH_NAMES = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];

@Pipe({
  name: 'monthName',
  standalone: true,
})
export class MonthNamePipe implements PipeTransform {
  transform(value: string | number | null | undefined | unknown): string {
    if (value == null || value === '' || value === '0') return 'غير محدد';
    const n = typeof value === 'string' ? parseInt(value, 10) : typeof value === 'number' ? value : NaN;
    if (n >= 1 && n <= 12) return MONTH_NAMES[n - 1];
    return String(value);
  }
}
