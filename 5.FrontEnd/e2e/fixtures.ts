import {BrowserContext,Page} from '@playwright/test';
export const id='11111111-1111-1111-1111-111111111111',owner='22222222-2222-2222-2222-222222222222';
export const longName='مريم اسم عربي طويل للاختبار مع بيانات إضافية واسم LatinVeryLongNameWithoutSpacesToCheckWrapping';
export const student={id,fullName:longName,phone:'01012345678',secondaryPhone:'+447700900123',address:'القاهرة، عنوان طويل في المنطقة لاختبار التفاف النص دون إخفاء المعلومات',area:'منطقة طويلة الاسم',college:'كلية الدراسات',academicYear:'السنة الثالثة',confessionFather:'أب الاعتراف',notes:'ملاحظات طويلة تحتوي على تفاصيل مهمة للمراجعة',gender:1,birthDate:null,birthMonth:5,servantId:owner,servantName:longName,version:'AAAAAAAABAE=',createdAt:'2026-10-01T08:00:00Z',updatedAt:'2026-10-01T08:00:00Z',lastAttendanceDate:'2026-10-09'};
export function session(app:string,overrides:Record<string,unknown>={}){const token='test.'+Buffer.from(JSON.stringify({exp:Math.floor(Date.now()/1000)+3600,sub:owner,role:app==='admin' ? 'Manager' : 'Secretary'})).toString('base64url')+'.signature';return {token,refreshToken:'mock-refresh',userId:owner,fullName:longName,role:app==='admin' ? 'Manager' : 'Secretary',requiresPasswordChange:false,requiresProfileCompletion:false,...overrides};}
export async function authenticate(context:BrowserContext,app:string,overrides:Record<string,unknown>={}){const value=session(app,overrides);await context.addInitScript(({app,value})=>{const key=`ydm.${app==='admin' ? 'adminapp' : 'servantapp'}.user`;if(!sessionStorage.getItem(key+'.fixture-seeded')){if(!localStorage.getItem(key))localStorage.setItem(key,JSON.stringify(value));sessionStorage.setItem(key+'.fixture-seeded','1');}},{app,value});}
export async function fixtures(context:BrowserContext,state:'populated'|'empty'|'failed'|'loading'='populated'){
 await context.route('**/*',async route=>{const url=new URL(route.request().url());if(url.hostname==='127.0.0.1' || url.protocol==='blob:' || url.protocol==='data:'){await route.continue();return;}
 if(!url.pathname.toLowerCase().includes('/api/')){await route.abort();return;}
 if(route.request().method()==='OPTIONS'){await route.fulfill({status:204,headers:{'access-control-allow-origin':'*','access-control-allow-headers':'*','access-control-allow-methods':'*'}});return;}
 if(state==='loading'){await new Promise(r=>setTimeout(r,1500));}
 if(state==='failed'){await route.fulfill({status:503,json:{message:'تعذر تحميل البيانات للاختبار'}});return;}
 const p=url.pathname.toLowerCase().replace(/^.*\/api\//,'');const empty=state==='empty';const items=empty ? [] : [student];const paged=(rows:any[],count=rows.length)=>({items:empty ? [] : rows,totalCount:empty ? 0 : count,page:Number(url.searchParams.get('page') || 1),pageSize:Number(url.searchParams.get('pageSize') || 25)});let body:any={};
 if(p.endsWith('/photo') && route.request().method()==='GET'){await route.fulfill({status:404,json:{message:'No photo'}});return;}
 if(p==='studentcommands/import-preview'){await route.fulfill({json:route.request().postDataJSON().rows.map((r:any)=>({row:r.row,candidates:[],error:null}))});return;}
 if(p.startsWith('auth/login') || p.startsWith('auth/refresh-token') || p.startsWith('auth/change-password')){const data=route.request().postDataJSON();let role='Manager';try{role=JSON.parse(Buffer.from(data.token.split('.')[1],'base64url').toString()).role;}catch{}body=session(role==='Secretary' ? 'servant' : 'admin',{refreshToken:'rotated-refresh'});}
 else if(p==='profile')body={...student,id:owner,userName:'test-user',email:'person@example.com',isActive:true,role:'Manager'};
 else if(p.includes('lookups/recipients'))body=paged([{id,name:longName,phone:student.phone,secondaryPhone:student.secondaryPhone}],51);
 else if(p.includes('lookups/'))body=paged([{id:owner,name:longName}],51);
 else if(p.includes('academic-years'))body=['السنة الثالثة','السنة الأولى'];
 else if(p.endsWith('areas'))body=['منطقة طويلة الاسم','القاهرة'];
 else if(p.includes('audit'))body=paged([]);
 else if(p.includes('studentqueries/students-for-servant'))body=paged(items.map(student=>({student,segment:'Assigned'})),51);
 else if(p==='studentcommands/archived')body=paged(items,51);
 else if(p.includes('studentqueries/paged'))body=paged(items,51);
 else if(p.includes('studentqueries/servant/'))body=items;
 else if(p.includes('studentqueries/'))body=student;
 else if(p.includes('activity-history/'))body=paged([{id,studentId:id,studentName:longName,callDate:'2026-10-01T08:00:00Z',visitDate:'2026-10-01T08:00:00Z',callStatus:1,visitOutcome:0,notes:student.notes,nextVisitDate:'2026-11-01T08:00:00Z',nextFollowUpDate:'2026-11-01T08:00:00Z',recordedByServantId:owner,recordedByName:longName,participants:[{servantId:owner,servantName:longName}],canCorrect:true,isVoided:false,version:student.version}],51);
 else if(p.startsWith('follow-up-tasks'))body={...paged([{id,studentId:id,studentName:longName,assigneeId:owner,assigneeName:longName,type:'Visit',status:'Open',dueAt:'2026-10-10T08:00:00Z',version:student.version}],51),dueStudents:1};
 else if(p==='meetings/settings')body={meetingDay:5,reportWeekStart:5,version:student.version};
 else if(p==='meetings/recurrence')body=[{date:'2026-10-16',session:null}];
 else if(p.startsWith('meetings/') && p.endsWith('eligibility'))body=[];
 else if(p.startsWith('meetings/') && p.endsWith('eligibility-candidates'))body=paged([{id,name:longName}],51);
 else if(p.startsWith('meetings/') && p.endsWith('roster'))body=paged([{id,personId:id,name:longName,present:false,assignedServantId:owner}],51);
 else if(p==='meetings')body=paged([{id,date:'2026-10-09',status:'Draft',rosterKnown:true,version:student.version}],51);
 else if(p.startsWith('approvals/'))body=paged([{...student,id,studentId:id,requestedByUserId:owner,requestedByUserName:longName,requestedAt:'2026-10-01T08:00:00Z',status:'Pending',processedAt:null}],51);
 else if(p.includes('requests'))body=[];
 else if(p.includes('notification/schedules'))body=empty ? [] : [{id,daysOfWeek:'5',timeOfDay:'18:00:00',isActive:true}];
 else if(p.includes('reports/suite/'))body={title:'تقرير المتابعة',period:'من 2026-10-01 إلى 2026-10-11 بتوقيت القاهرة',columns:[{key:'name',label:'الطالب',type:'text'},{key:'owner',label:'المسؤول',type:'text'},{key:'lastAttempt',label:'آخر محاولة',type:'datetime'}],rows:empty ? [] : [{name:longName,owner:longName,lastAttempt:'2026-10-01T08:00:00Z'}],totalCount:empty ? 0 : 51,page:1,pageSize:25,totals:{roster:empty ? 0 : 51,attempted:empty ? 0 : 12,coverage:empty ? null : 23.5}};
 else if(p.includes('reports/dashboard'))body={totalStudents:51,totalServants:51,callsThisWeek:12,visitsThisWeek:3,studentsNeedingFollowUp:4,studentsNotContacted:8,unassignedStudents:1,studentsNeedingVisit:2};
 else if(p.includes('reports/servant-stats'))body=[{servantId:owner,assignedStudentsCount:51,lastCallDate:'2026-10-01T08:00:00Z',lastVisitDate:null}];
 else if(p.includes('reports/servant/') && p.endsWith('performance'))body={servantId:owner,fullName:longName,assignedStudentsCount:51,callsThisWeek:12,visitsThisWeek:3};
 else if(p.includes('reports/'))body=paged([{...student,name:longName,key:'القاهرة',count:51,studentId:id,studentName:longName,servantId:owner,servantName:longName,assignedStudentsCount:51,callsThisWeek:12,visitsThisWeek:3,callsCount:12,visitsCount:3,totalActivities:15}],51);
 else if(p.includes('users/paged'))body=paged([{...student,id:owner,userName:'user',email:'long.email@example.com',role:'Servant',isActive:true}],51);
 else if(p.startsWith('users/'))body={...student,id:owner,userName:'user',role:'Servant',isActive:true};
 else if(p==='users')body=[{...student,id:owner,userName:'user',role:'Servant',isActive:true}];
 else if(p==='followup/servants')body=[{id:owner,fullName:longName}];
 else if(p.includes('history'))body=[];
 if(route.request().method()!=='GET' && !p.startsWith('auth/'))body={};
 await route.fulfill({status:200,json:body,headers:{'access-control-allow-origin':'*'}});
 });
}
export async function noOverflow(page:Page){return await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth,wide:Array.from(document.querySelectorAll('body *')).filter(e=>e.scrollWidth>e.clientWidth+2 && e.clientWidth>0 && getComputedStyle(e).display!=='none' && !e.closest('dialog:not([open]),.sidebar:not(.open),.ydm-table-region,.table-container,.table-wrapper')).slice(-12).map(e=>({tag:e.tagName,class:e.className,client:e.clientWidth,scroll:e.scrollWidth,text:e.textContent?.slice(0,100)})),offenders:Array.from(document.querySelectorAll('body *')).filter(e=>{const r=e.getBoundingClientRect(),s=getComputedStyle(e);return s.display!=='none' && s.visibility!=='hidden' && r.width>0 && (r.right>innerWidth+2 || r.left < -2) && !e.closest('dialog:not([open]),.sidebar:not(.open),.ydm-table-region,.table-container,.table-wrapper');}).slice(0,12).map(e=>({tag:e.tagName,class:e.className,width:Math.round(e.getBoundingClientRect().width)}))}));}
