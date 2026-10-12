import http from 'node:http';
import {readFile,stat} from 'node:fs/promises';
import path from 'node:path';
const roots={admin:path.resolve('AdminApp/dist/AdminApp/browser'),servant:path.resolve('ServantApp/dist/ServantApp/browser')};
const versions={admin:0,servant:0};
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.json':'application/json','.webmanifest':'application/manifest+json','.svg':'image/svg+xml','.png':'image/png','.ico':'image/x-icon','.woff2':'font/woff2'};
http.createServer(async(req,res)=>{
 try{const url=new URL(req.url,'http://localhost');if(url.pathname==='/health'){res.end('ok');return;}
 if(url.pathname.startsWith('/__test/api/')){
  res.setHeader('Content-Type','application/json');res.setHeader('Cache-Control','no-store');
  if(url.pathname.toLowerCase()==='/__test/api/profile')res.end(JSON.stringify({id:'22222222-2222-2222-2222-222222222222',fullName:'Local PWA profile',phone:'01012345678',address:'Cairo',email:'test@example.com'}));
  else if(url.pathname.endsWith('/photo')){res.writeHead(404);res.end('{}');}
  else res.end(JSON.stringify({items:[],totalCount:0,page:1,pageSize:25}));return;
 }
 if(url.pathname==='/__test/version'){const app=url.searchParams.get('app');if(!(app in versions)){res.writeHead(400);res.end();return;}versions[app]=Number(url.searchParams.get('version') || 0);res.end('ok');return;}
 if(url.pathname==='/YouthDataManager/' || url.pathname==='/YouthDataManager/index.html'){res.writeHead(200,{'Content-Type':'text/html','Cache-Control':'no-store'});res.end(await readFile(path.resolve('pages/index.html')));return;}
 let [,app,...parts]=url.pathname.split('/');let deployed=false;if(app==='YouthDataManager'){deployed=true;app=parts.shift();}if(!roots[app]){res.writeHead(404);res.end();return;}
 const root=deployed ? path.resolve('pages',app) : roots[app];let file=path.resolve(root,decodeURIComponent(parts.join('/')) || 'index.html');if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
 try{if(!(await stat(file)).isFile())file=path.join(root,'index.html');}catch{if(path.extname(file)){res.writeHead(404);res.end();return;}file=path.join(root,'index.html');}
 let body=await readFile(file);if(deployed && file.endsWith('ngsw.json') && versions[app]){const manifest=JSON.parse(body.toString());manifest.appData={verificationVersion:versions[app]};body=Buffer.from(JSON.stringify(manifest));}if(file.endsWith('index.html'))body=Buffer.from(body.toString().replace('<base href="/">',`<base href="/${app}/">`));
 res.writeHead(200,{'Content-Type':mime[path.extname(file)] || 'application/octet-stream','Cache-Control':'no-store'});res.end(body);
 }catch{res.writeHead(500);res.end();}
}).listen(4173,'127.0.0.1');
