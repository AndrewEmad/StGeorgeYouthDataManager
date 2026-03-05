import { Pipe, PipeTransform } from '@angular/core';
import { VISIT_OUTCOME_OPTIONS } from '../models/follow-up.model';

@Pipe({ name: 'visitOutcomeLabel', standalone: true })
export class VisitOutcomeLabelPipe implements PipeTransform {
  transform(outcome: number): string {
    return VISIT_OUTCOME_OPTIONS.find((o) => o.value === outcome)?.label ?? String(outcome);
  }
}
