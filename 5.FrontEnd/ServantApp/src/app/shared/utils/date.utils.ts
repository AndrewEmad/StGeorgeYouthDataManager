/**
 * Shared date utilities for consistent ISO handling across the app.
 */

/**
 * Returns date-only string (YYYY-MM-DD) from a Date or ISO string.
 * Use for date inputs and API payloads that expect date only.
 */
export function toDateOnly(value: Date | string | null | undefined): string {
  if (value == null) return '';
  const d = typeof value === 'string' ? new Date(value) : value;
  if (isNaN(d.getTime())) return '';
  return d.toISOString().split('T')[0] ?? '';
}

/**
 * Returns full ISO date-time string from a Date or ISO string.
 * Use for API payloads that expect datetime.
 */
export function toIsoDateTime(value: Date | string | null | undefined): string {
  if (value == null) return '';
  const d = typeof value === 'string' ? new Date(value) : value;
  if (isNaN(d.getTime())) return '';
  return d.toISOString();
}

/**
 * Returns ISO string suitable for datetime-local input (slice to 16 chars: YYYY-MM-DDTHH:mm).
 */
export function toDateTimeLocalValue(value: Date | string | null | undefined): string {
  if (value == null) return '';
  const d = typeof value === 'string' ? new Date(value) : value;
  if (isNaN(d.getTime())) return '';
  return d.toISOString().slice(0, 16);
}

/**
 * Parses an ISO date or datetime string to date-only (YYYY-MM-DD).
 * Useful when API returns full ISO and we need to bind to a date input.
 */
export function fromIsoToDateOnly(iso: string | null | undefined): string {
  if (iso == null || !iso) return '';
  const part = iso.split('T')[0];
  return part ?? '';
}
