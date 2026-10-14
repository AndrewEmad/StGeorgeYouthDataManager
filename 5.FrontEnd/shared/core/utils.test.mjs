import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import ts from '../../node_modules/typescript/lib/typescript.js';
const code = ts.transpileModule(readFileSync(new URL('./utils.ts',import.meta.url),'utf8'),{compilerOptions:{module:ts.ModuleKind.ES2022,target:ts.ScriptTarget.ES2022}}).outputText;
const u = await import(`data:text/javascript;base64,${Buffer.from(code).toString('base64')}`);
test('Cairo local datetime round trip preserves an unchanged timestamp',()=>{
  process.env.TZ='Africa/Cairo'; const iso='2026-10-11T12:15:00.000Z';
  assert.equal(u.isoDatetime(u.datetimeLocal(iso)),iso);
  assert.equal(u.datetimeLocal('invalid'),''); assert.equal(u.isoDatetime(''),'');
  assert.equal(u.dateOnly('2000-01-15T00:00:00Z'),'2000-01-15');
});
test('phone actions preserve explicit international country codes',()=>{
  assert.equal(u.phoneInternational('01012345678'),'201012345678');
  assert.equal(u.phoneInternational('+20 1012345678'),'201012345678');
  assert.equal(u.phoneInternational('+44 7700 900123'),'447700900123');
  assert.equal(u.phoneInternational('٠١٠١٢٣٤٥٦٧٨'),'201012345678');
});
test('CSV preserves Arabic, multiline fields, escaped quotes and physical row numbers',()=>{
  const rows=u.parseCsv('\ufeffName,Notes\r\nأحمد,"line1\nline2 ""quoted"""\r\nمريم,note');
  assert.deepEqual(rows[1],{values:['أحمد','line1\nline2 "quoted"'],line:2}); assert.equal(rows[2].line,4);
  assert.throws(()=>u.parseCsv('a,"unterminated'));
  const csv=u.csvDocument(['Name','Notes'],[['+2010','a\nb']]);
  assert.ok(csv.startsWith('\ufeff"Name","Notes"\r\n')); assert.ok(csv.includes('"\'+2010"')); assert.equal(csv.match(/\ufeff/g).length,1);
});

test('Cairo form conversion is independent of device timezone and rejects invalid calendar or DST gap values',()=>{
 process.env.TZ='America/New_York';assert.equal(u.datetimeLocal('2026-10-11T12:15:00Z'),'2026-10-11T15:15');assert.equal(u.isoDatetime('2026-10-11T15:15'),'2026-10-11T12:15:00.000Z');
 assert.equal(u.isoDatetime('2026-04-24T00:30'),'');assert.equal(u.isoDatetime('2026-02-30T12:00'),'');assert.equal(u.isoDatetime('2026-10-11T25:00'),'');
 const original='2026-10-29T21:30:24.123Z';assert.equal(u.isoDatetime(u.datetimeLocal(original),original),original);
 assert.throws(()=>u.parseCsv('a,"b"c'));assert.throws(()=>u.parseCsv('a,b"c'));
});

test('service years strictly exclude September 12 even when it is Friday',()=>{
  assert.deepEqual(u.serviceYearPeriod(2026),{dateFrom:'2026-09-18',dateTo:'2027-09-10'});
  assert.deepEqual(u.serviceYearPeriod(2025),{dateFrom:'2025-09-19',dateTo:'2026-09-11'});
  assert.deepEqual(u.serviceYearPeriod(2024),{dateFrom:'2024-09-13',dateTo:'2025-09-05'});
  for(const zone of ['America/New_York','Pacific/Auckland','Africa/Cairo']){process.env.TZ=zone;assert.equal(u.serviceYearPeriod(2026).dateFrom,'2026-09-18');}
  for(const year of [1899,9998,2026.5,NaN])assert.throws(()=>u.serviceYearPeriod(year));
});

const academicCode=ts.transpileModule(readFileSync(new URL('./academic-years.ts',import.meta.url),'utf8'),{compilerOptions:{module:ts.ModuleKind.ES2022,target:ts.ScriptTarget.ES2022}}).outputText;
const academic=await import(`data:text/javascript;base64,${Buffer.from(academicCode).toString('base64')}`);
test('academic year list and known spelling variants use the exact canonical labels',()=>{
 assert.deepEqual([...academic.academicYears],['سنة اولى','سنة ثانية','سنة ثالثة','سنة رابعة','سنة خامسة','سنة سادسة','سنة سابعة','حديث التخرج','غير معروف']);
 for(const value of ['اولى','أولي',' سنة أولي ','السنة الأُولَى'])assert.equal(academic.canonicalAcademicYear(value),'سنة اولى');
 assert.equal(academic.canonicalAcademicYear('الفرقة الرابعة بالجامعة'),'سنة رابعة');assert.equal(academic.canonicalAcademicYear('خريج حديث'),'حديث التخرج');
});
test('unresolved stages stay unresolved while empty edit values stay optional',()=>{
 for(const value of ['أولى ثانوي','سنة رابعة (ثانوي)','سنة ثامنة','constructor','__proto__']){assert.equal(academic.canonicalAcademicYear(value),null);assert.equal(academic.academicYearForEdit(value),undefined);}
 assert.equal(academic.academicYearForEdit(''),null);assert.equal(academic.academicYearForEdit(null),null);
});

test('graduate values and unknown choice follow the reviewed normalization rules',()=>{
 for(const value of ['خريجين','الخريجين','خريجة','اتخرجت 2025','حديثي التخرج','خريج '])assert.equal(academic.canonicalAcademicYear(value),'حديث التخرج');
 assert.equal(academic.canonicalAcademicYear('غير معروف'),'غير معروف');assert.equal(academic.academicYearForEdit('غير معروف'),'غير معروف');
 assert.equal(academic.canonicalAcademicYear('سنة رابعه  ( هيتخرج ٢٠٢٧'),'سنة رابعة');
});

const areaCode=ts.transpileModule(readFileSync(new URL('./student-areas.ts',import.meta.url),'utf8'),{compilerOptions:{module:ts.ModuleKind.ES2022,target:ts.ScriptTarget.ES2022}}).outputText;
const area=await import(`data:text/javascript;base64,${Buffer.from(areaCode).toString('base64')}`);
test('approved areas normalize spelling and preserve distinct neighborhoods',()=>{
 assert.equal(area.studentAreas.length,40);assert.equal(new Set(area.studentAreas).size,40);
 for(const value of area.studentAreas)assert.equal(area.canonicalStudentArea(value),value);
 for(const [input,expected] of [['إبراهيم بك','ابراهيم بك'],['المرج الجديده','المرج الجديدة'],['المعادي الجديده','المعادى الجديدة'],['عزبة رستم','عزبه رستم'],['كوبري عرابي','كوبرى عرابى'],['أخرى','اخرى']])assert.equal(area.canonicalStudentArea(input),expected);
 assert.notEqual(area.canonicalStudentArea('شبرا البلد'),area.canonicalStudentArea('شبرا مصر'));
});
test('unapproved nonempty areas become other while blank values stay optional',()=>{
 for(const value of ['منطقة قديمة','غير معروف','constructor','__proto__']){assert.equal(area.canonicalStudentArea(value),'اخرى');assert.equal(area.studentAreaForEdit(value),'اخرى');}
 assert.equal(area.canonicalStudentArea(''), '');assert.equal(area.canonicalStudentArea('  '), '');assert.equal(area.canonicalStudentArea(null), '');
 assert.equal(area.studentAreaForEdit(''),null);assert.equal(area.studentAreaForEdit(null),null);
});

test('reviewed neighborhood aliases use the user-approved canonical destinations',()=>{
 for(const [input,expected] of [["شبرا المحطة", "شبرا البلد"], ["شبرا الخيمة", "شبرا البلد"], ["شبرا الخيمه", "شبرا البلد"], ["ارض نوبار شبرا الخيمه", "ارض نوبار"], ["المساكن", "الوحدة"], ["الملف شبرا الخيمه", "الملف"], ["الوحدة عند المصانع", "الوحدة"], ["خلف الكنيسة", "شبرا البلد"], ["شارع زكريا", "شبرا البلد"], ["منية السيرج", "اخرى"]]){
  assert.equal(area.canonicalStudentArea(input),expected);assert.equal(area.studentAreaForEdit(input),expected);
  assert.ok(area.studentAreas.includes(expected));assert.ok(!area.studentAreas.includes(input));
 }
 assert.equal(area.canonicalStudentArea('أرض نوبار شبرا الخيمة'),'ارض نوبار');
 assert.equal(area.canonicalStudentArea('ترعة زكريا'),'اخرى');
});
