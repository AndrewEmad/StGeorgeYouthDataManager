/** Calendar dates are preserved, never translated through UTC midnight. */
export function dateOnly(value: string | Date | null | undefined): string {
  if (!value) return '';
  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}(?:T|$)/.test(value)) {const prefix=value.slice(0,10);const d=new Date(prefix+'T12:00:00Z');return Number.isFinite(d.getTime()) && d.toISOString().slice(0,10)===prefix ? prefix : '';}
  const d = value instanceof Date ? value : new Date(value);
  if (!Number.isFinite(d.getTime())) return '';
  return `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}`;
}
const pad = (n: number) => String(n).padStart(2,'0');
const cairoClock = new Intl.DateTimeFormat('en-CA',{timeZone:'Africa/Cairo',year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'});
export function datetimeLocal(value: string | Date | null | undefined): string {
  if (!value) return '';
  const d=value instanceof Date ? value : new Date(value.includes('T') && !/(Z|[+-]\d{2}:?\d{2})$/.test(value) ? value+'Z' : value);
  if(!Number.isFinite(d.getTime()))return '';
  const parts=Object.fromEntries(cairoClock.formatToParts(d).map(p=>[p.type,p.value]));
  return `${parts['year']}-${parts['month']}-${parts['day']}T${parts['hour']}:${parts['minute']}`;
}
/** Native form values are Cairo wall times, independent of the device timezone.
 * Preserve an unchanged original timestamp, including seconds and a repeated DST hour. */
export function isoDatetime(value:string|Date|null|undefined,original?:string|null):string {
  if(!value)return '';
  if(value instanceof Date)return Number.isFinite(value.getTime()) ? value.toISOString() : '';
  if(!dateOnly(value))return '';
  if(/(Z|[+-]\d{2}:?\d{2})$/.test(value)){const d=new Date(value);return Number.isFinite(d.getTime()) ? d.toISOString() : '';}
  const match=/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,3}))?)?$/.exec(value);
  if(!match || Number(match[4])>23 || Number(match[5])>59 || Number(match[6] || 0)>59)return '';
  if(original && datetimeLocal(original)===value.slice(0,16)){const d=new Date(/(Z|[+-]\d{2}:?\d{2})$/.test(original) ? original : original+'Z');if(Number.isFinite(d.getTime()))return d.toISOString();}
  const wall=Date.UTC(+match[1],+match[2]-1,+match[3],+match[4],+match[5],+(match[6] || 0),+(match[7] || '0').padEnd(3,'0'));
  // Cairo uses UTC+2/+3. Validate candidates against timezone rules, rejecting the spring gap.
  const candidates=[wall-3*3600000,wall-2*3600000].filter(t=>datetimeLocal(new Date(t))===value.slice(0,16));
  return candidates.length ? new Date(candidates[0]).toISOString() : '';
}
export function phoneInternational(value: string): string {
  const text=(value ?? '').trim().replace(/[٠-٩]/g,c=>String(c.charCodeAt(0)-1632)).replace(/[۰-۹]/g,c=>String(c.charCodeAt(0)-1776));
  if(!/^\+?[0-9\s\u0085().-]+$/.test(text))return '';
  let digits=text.replace(/\D/g,'');
  if(text.startsWith('+')){}else if(digits.startsWith('00'))digits=digits.slice(2);
  else if(digits.startsWith('0') && digits.length===11)digits='2'+digits;
  else if(digits.startsWith('1') && digits.length===10)digits='20'+digits;
  return digits.length>=7 && digits.length<=15 ? digits : '';

}
export function csvCell(value: unknown): string {
  let s = value == null ? '' : String(value);
  if (/^[\s]*[=+@-]/.test(s)) s = `'${s}`;
  return `"${s.replace(/"/g,'""')}"`;
}
export function csvDocument(headers: string[], rows: unknown[][]): string {
  return '\ufeff' + [headers,...rows].map(row => row.map(csvCell).join(',')).join('\r\n');
}
/** RFC4180 records with physical source line numbers, including multiline fields. */
export function parseCsv(text: string): { values: string[]; line: number }[] {
  const records: { values: string[]; line: number }[] = [];
  let row: string[] = [], field = '', quoted = false, closed = false, line = 1, start = 1;
  const finish = () => { row.push(field); if (row.some(v=>v.trim())) records.push({values:row,line:start}); row=[]; field=''; closed=false; start=line+1; };
  text = text.replace(/^\ufeff/,'');
  for (let i=0;i<text.length;i++) {
    const c=text[i];
    if(c==='"') { if(quoted && text[i+1]==='"') {field+='"';i++;} else if(!quoted && field==='') quoted=true; else if(quoted) {quoted=false;closed=true;} else throw new Error(`Invalid quote at line ${line}`); }
    else if(c===',' && !quoted) {row.push(field);field='';closed=false;}
    else if((c==='\r'||c==='\n') && !quoted) {if(c==='\r'&&text[i+1]==='\n')i++;finish();line++;}
    else {if(closed)throw new Error(`Unexpected character after quoted field at line ${line}`);field+=c;if(c==='\n')line++;}
  }
  if(quoted) throw new Error(`Unclosed quoted field at line ${start}`);
  if(field || row.length)finish();
  return records;
}

/** HTTP headers require ASCII bytes; the API decodes this explicit transport marker. */
export function auditReasonHeaders(reason:string):Record<string,string> {
  return {'X-Audit-Reason':encodeURIComponent(reason),'X-Audit-Reason-Encoding':'uri'};
}
