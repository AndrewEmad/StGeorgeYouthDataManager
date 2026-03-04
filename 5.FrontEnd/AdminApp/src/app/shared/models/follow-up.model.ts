export interface CallLogDto {
  id: string;
  studentId: string;
  studentName: string;
  callDate: string;
  callStatus: number;
  notes: string;
  nextFollowUpDate: string | null;
}

export interface HomeVisitParticipantDto {
  servantId: string;
  servantName: string;
}

export interface HomeVisitDto {
  id: string;
  studentId: string;
  studentName: string;
  visitDate: string;
  visitOutcome: number;
  notes: string;
  nextVisitDate: string | null;
  participants?: HomeVisitParticipantDto[];
  recordedByServantId?: string;
}
