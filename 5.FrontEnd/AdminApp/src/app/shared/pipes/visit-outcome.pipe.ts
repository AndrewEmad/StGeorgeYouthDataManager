import { Pipe, PipeTransform } from '@angular/core';

const VISIT_OUTCOME_MAP: Record<number, string> = {
  0: 'تمت الزيارة',
  1: 'غير موجود',
  2: 'رفض الاستقبال',
  3: 'مؤجلة',
};

@Pipe({
  name: 'visitOutcome',
  standalone: true,
})
export class VisitOutcomePipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null) return '—';
    return VISIT_OUTCOME_MAP[value] ?? String(value);
  }
}
