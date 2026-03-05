import type { SelectOption } from './common.model';

/**
 * Call status enum (matches backend).
 */
export const CallStatus = {
  NoAnswer: 0,
  Answered: 1,
  Busy: 2,
  Closed: 3,
  WrongNumber: 4,
} as const;
export type CallStatus = (typeof CallStatus)[keyof typeof CallStatus];

/**
 * Visit outcome enum (matches backend).
 */
export const VisitOutcome = {
  Success: 0,
  NotPresent: 1,
  Declined: 2,
  Postponed: 3,
} as const;
export type VisitOutcome = (typeof VisitOutcome)[keyof typeof VisitOutcome];

/**
 * Call record from history API.
 */
export interface CallRecord {
  id: string;
  studentId: string;
  callDate: string;
  callStatus: number;
  notes?: string;
  nextFollowUpDate?: string;
}

/**
 * Participant in a visit.
 */
export interface VisitParticipant {
  servantId: string;
  fullName?: string;
}

/**
 * Visit record from history API.
 */
export interface VisitRecord {
  id: string;
  studentId: string;
  visitDate: string;
  visitOutcome: number;
  notes?: string;
  nextVisitDate?: string;
  participants?: VisitParticipant[];
  recordedByServantId?: string;
}

/**
 * Payload for registering a call.
 */
export interface RegisterCallDto {
  studentId: string;
  callDate: string;
  callStatus: number;
  notes?: string;
  nextFollowUpDate?: string | null;
}

/**
 * Payload for registering a visit.
 */
export interface RegisterVisitDto {
  studentId: string;
  visitDate: string;
  visitOutcome: number;
  notes?: string;
  nextVisitDate?: string | null;
  participantServantIds?: string[] | null;
}

/**
 * Payload for updating a visit.
 */
export interface UpdateVisitDto {
  visitDate: string;
  visitOutcome: number;
  notes: string;
  nextVisitDate: string | null;
  participantServantIds: string[] | null;
}

/**
 * Servant summary (id + fullName) for participant lists.
 */
export interface ServantSummary {
  id: string;
  fullName: string;
}

/**
 * Options for call status select (label/value).
 */
export const CALL_STATUS_OPTIONS: SelectOption<number>[] = [
  { value: 0, label: 'لم يجب' },
  { value: 1, label: 'أجاب' },
  { value: 2, label: 'مشغول' },
  { value: 3, label: 'مغلق' },
  { value: 4, label: 'رقم خاطئ' },
];

/**
 * Options for visit outcome select (label/value).
 */
export const VISIT_OUTCOME_OPTIONS: SelectOption<number>[] = [
  { value: 0, label: 'ناجحة' },
  { value: 1, label: 'غير موجود' },
  { value: 2, label: 'رفض الزيارة' },
  { value: 3, label: 'مؤجلة' },
];
