import {mkdtempSync,cpSync,mkdirSync,readFileSync,writeFileSync,readdirSync,existsSync,renameSync} from 'node:fs';
import {join,resolve,dirname,sep} from 'node:path';
import {fileURLToPath} from 'node:url';
import {tmpdir} from 'node:os';
import {execFileSync} from 'node:child_process';
import {createHash,randomUUID} from 'node:crypto';
const repository=process.argv[2];
const fixtureApi=process.argv.find(a=>a.startsWith('--fixture-api='))?.slice('--fixture-api='.length);
if(fixtureApi && new URL(fixtureApi).hostname!=='127.0.0.1')throw new Error('Fixture API must be local.');
if(!repository || !/^[a-zA-Z0-9._-]+$/.test(repository))throw new Error('Provide the repository path segment.');
const root=resolve(dirname(fileURLToPath(import.meta.url)),'..'), staging=mkdtempSync(join(tmpdir(),'ydm-pages-'));
const artifactRoot=join(resolve(root,'..'),'artifacts');mkdirSync(artifactRoot,{recursive:true});
const assembled=mkdtempSync(join(artifactRoot,'pages-stage-'));const published=join(root,'pages');
const within=(path,parent)=>resolve(path).startsWith(resolve(parent)+sep);
if(!within(assembled,artifactRoot) || published!==join(root,'pages'))throw new Error('Unsafe publication paths.');
for(const [app,folder] of [['AdminApp','admin'],['ServantApp','servant']]){
 const base=`/${repository}/${folder}/`;
 execFileSync(process.execPath,[join(root,'node_modules/@angular/cli/bin/ng.js'),'build','--configuration','production','--base-href',base,'--output-path',join(staging,folder)],{cwd:join(root,app),stdio:'inherit'});
 const source=join(staging,folder,'browser');const html=readFileSync(join(source,'index.html'),'utf8');
 if(!html.includes(`<base href="${base}">`))throw new Error('Incorrect deployed base path.');
 const manifest=JSON.parse(readFileSync(join(source,'manifest.webmanifest'),'utf8'));
 if(manifest.start_url!=='./' || manifest.scope!=='./')throw new Error('Manifest must be relative to its deployment.');
 const ngsw=JSON.parse(readFileSync(join(source,'ngsw.json'),'utf8'));
 if(fixtureApi){
  const environment=readFileSync(join(root,app,'src/environments/environment.ts'),'utf8');
  const configured=environment.match(/apiUrl:\s*['"]([^'"]+)['"]/)[1];
  for(const file of readdirSync(source).filter(f=>f.endsWith('.js'))){const original=readFileSync(join(source,file),'utf8');const replacement=original.replaceAll(configured,fixtureApi);if(replacement!==original){writeFileSync(join(source,file),replacement);ngsw.hashTable[base+file]=createHash('sha1').update(replacement).digest('hex');}}
  writeFileSync(join(source,'ngsw.json'),JSON.stringify(ngsw));
 }
 if(!ngsw.index.startsWith(base) || Object.keys(ngsw.hashTable).some(k=>!k.startsWith(base)))throw new Error('Service worker contains a mismatched deployment scope.');
 cpSync(source,join(assembled,folder),{recursive:true});
}
const style=readdirSync(join(assembled,'admin')).find(f=>/^styles-.*\.css$/.test(f));
writeFileSync(join(assembled,'index.html'),`<!doctype html><html lang="ar" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>خدمة الشباب</title><link rel="stylesheet" href="./admin/${style}"></head><body><main class="ydm-page"><section class="glass-card ydm-panel"><h1>خدمة الشباب</h1><p>اختر تطبيق الخدمة المناسب لحسابك.</p><div class="ydm-toolbar"><a class="btn-primary" href="./admin/">الإدارة</a><a class="btn-outline" href="./servant/">الخدام</a></div></section></main></body></html>`);
// Publish only after both complete builds verify, without merging obsolete or fixture assets.
let previous=null;
if(existsSync(published)){
 previous=join(artifactRoot,'pages-previous-'+randomUUID());
 if(!within(published,root) || !within(previous,artifactRoot))throw new Error('Unsafe publication backup paths.');
 renameSync(published,previous);
}
try{renameSync(assembled,published);}catch(error){if(previous && !existsSync(published))renameSync(previous,published);throw error;}
console.log('Both applications packaged as one fresh publication and deployment paths verified.');
