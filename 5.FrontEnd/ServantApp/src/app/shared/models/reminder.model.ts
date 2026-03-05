/**
 * Notification schedule from API.
 */
export interface ScheduleDto {
  id: string;
  daysOfWeek: string;
  timeOfDay: string;
  isActive: boolean;
}

/**
 * Payload for creating a schedule.
 */
export interface CreateScheduleRequest {
  daysOfWeek: string;
  timeOfDay: string;
}
