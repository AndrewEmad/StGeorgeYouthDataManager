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
