// Fictional, deterministic data for the user manual. No live API is contacted.
import {BrowserContext} from '@playwright/test';
import {readFileSync} from 'node:fs';
import {fixtures,id,owner,session} from './fixtures';
import {reportDefinitions} from '../shared/core/report-catalog';
export {id,owner};
export const second='33333333-3333-3333-3333-333333333333';
const version='AAAAAAAABAE=';
const student={id,fullName:'مينا سامح',phone:'01012345678',secondaryPhone:'+201000001122',address:'عنوان تجريبي — شبرا البلد',area:'شبرا البلد',college:'كلية التجارة',academicYear:'سنة ثالثة',confessionFather:'أب الاعتراف',notes:'مثال توضيحي: يفضل التواصل مساءً.',gender:0,birthDate:'2004-01-15',birthMonth:1,servantId:owner,servantName:'يوسف جرجس',version,createdAt:'2026-09-20T08:00:00Z',updatedAt:'2026-10-14T08:00:00Z',lastAttendanceDate:'2026-10-09'};
const other={...student,id:second,fullName:'مريم عادل',phone:'01000011223',secondaryPhone:null,address:'',academicYear:'سنة ثانية',gender:1,birthDate:null,birthMonth:5};
const person={...student,id:owner,userName:'servant.demo',fullName:'يوسف جرجس',email:'demo@example.invalid',role:'Servant',isActive:true};
const meeting={id,date:'2026-10-09',status:'Draft',rosterKnown:true,version};
const task={id,studentId:id,studentName:student.fullName,assigneeId:owner,assigneeName:person.fullName,type:'Visit',status:'Open',dueAt:'2026-10-16T15:00:00Z',sourceType:'Manual',reason:null,version};
const call={id,studentId:id,studentName:student.fullName,callDate:'2026-10-12T16:00:00Z',visitDate:'2026-10-10T15:00:00Z',callStatus:1,visitOutcome:0,notes:'تم الاطمئنان عليه وتذكيره بموعد الاجتماع.',nextFollowUpDate:'2026-10-20T15:00:00Z',nextVisitDate:'2026-10-20T15:00:00Z',recordedByServantId:owner,recordedByName:person.fullName,participants:[{servantId:owner,servantName:person.fullName},{servantId:second,servantName:'بطرس عادل'}],canCorrect:true,isVoided:false,version};
const request={...student,kind:'addition',id,studentId:null,requestedByUserId:owner,requestedByUserName:person.fullName,requestedAt:'2026-10-12T10:00:00Z',status:'Pending',processedAt:null,processingReason:null,processedByUserName:null,createdStudentId:null,removalType:0};
const sources=['Suite','Pastoral'].map(part=>readFileSync(`../2.Infrastructure/YouthDataManager.Infrastructure/Reports/ReportDataProvider.${part}.cs`,'utf8')).join('\n');
const methods:Record<string,string>={attendance:'AttendanceTable','servant-attendance':'AttendanceTable','attendance-history':'AttendanceHistoryTable','servant-attendance-history':'AttendanceHistoryTable','group-attendance':'GroupAttendanceTable','meeting-trends':'MeetingTable','servant-service-summary':'ServantServiceTable','pastoral-follow-up':'PastoralFollowUpTable',contact:'ContactTable','follow-up':'TaskTable','workload-owners':'OwnerWorkloadTable',workload:'WorkloadTable','data-quality':'QualityTable',approvals:'ApprovalTable'};
function report(key:string){
 const start=sources.indexOf('private async Task<ReportTable> '+methods[key]+'('),tail=sources.slice(start),end=tail.indexOf('\n    private ',10),body=end<0 ? tail : tail.slice(0,end);
 const columns=Array.from(body.matchAll(/new\("([a-zA-Z]+)","([^"]+)"(?:,"([^"]+)")?\)/g)).map(m=>({key:m[1],label:m[2],type:m[3] || 'text'}));
 if(start<0 || !columns.length)throw new Error('Manual report columns missing: '+key);
 const attendance=key.includes('attendance') && !key.includes('history');
 let rows:any[]=[{id,studentId:id,personId:id,ownerId:owner,name:student.fullName,owner:person.fullName,requesterName:person.fullName,processedByName:null,area:student.area,year:student.academicYear,phone:student.phone,secondaryPhone:student.secondaryPhone,active:true,ownerActive:true,assignedStudentsCount:2,students:2,eligible:4,present:3,absent:1,rate:75,consecutiveAbsences:0,lastPresent:'2026-10-09',lastAttempt:'2026-10-12T16:00:00Z',lastSuccess:'2026-10-12T16:00:00Z',calls:3,callAnswered:1,callNotAnswered:2,callBusy:0,callSwitchedOff:0,callWrongNumber:0,visits:1,visitSuccessful:1,visitNotAtHome:0,visitRefusedVisit:0,visitPostponed:0,attempted:2,reached:true,attempts:3,openTasks:2,overdueTasks:1,tasks:2,type:'Visit',status:'Open',dueAt:task.dueAt,completedAt:null,source:'Manual',reason:'غياب متتالي، لا يوجد تواصل ناجح حديث',date:'2026-10-09',rosterKnown:true,servantEligible:2,servantPresent:1,servantRate:50,kind:'addition',requestedAt:'2026-10-12T10:00:00Z',processedAt:null,missingPhone:false,missingAddress:false,missingArea:false,missingYear:false,missingBirthday:false,duplicate:false}];
 let totals:Record<string,number|null>={total:1};
 if(key==='attendance' || key==='contact' || key==='data-quality' || key==='pastoral-follow-up' || key==='workload')rows.push({...rows[0],id:second,studentId:second,personId:second,name:other.fullName,phone:other.phone,year:other.academicYear,present:1,rate:25,consecutiveAbsences:3,lastPresent:'2026-09-18',lastSuccess:null,attempts:1,reached:false,missingAddress:true});
 if(attendance){if(key==='servant-attendance')rows[0]={...rows[0],id:owner,name:person.fullName};totals={eligible:rows.reduce((s,r)=>s+r.eligible,0),present:rows.reduce((s,r)=>s+r.present,0)};totals['rate']=Math.round(1000*Number(totals['present'])/Number(totals['eligible']))/10;}
 if(key.includes('history')){rows[0].present=true;if(key.startsWith('servant')){rows[0].personId=owner;rows[0].name=person.fullName;}}
 if(key==='contact')totals={roster:2,attempted:2,reached:1,coverage:50,neverAttempted:0,noRecentAttempt:0,noRecentSuccess:1,callAnswered:1,callNotAnswered:2,visitSuccessful:1};
 if(key==='group-attendance')rows[0]={...rows[0],id:owner,name:person.fullName,eligible:8,present:4,absent:4,rate:50};
 if(key==='servant-service-summary'){rows[0].id=owner;rows[0].name=person.fullName;rows[0].reached=1;totals={owners:1,eligible:4,present:3,rate:75,calls:3,visits:1,visitCredits:1,openTasks:2,overdueTasks:1};}
 if(key==='workload-owners'){rows[0]={...rows[0],id:owner,name:person.fullName,eligible:2,attempted:2,present:1,rate:50};totals={roster:2,owners:1,attempted:2,reached:1,coverage:50};}
 if(key==='follow-up')totals={open:1,completed:0,cancelled:0,pendingReview:0,dueStudents:0};
 if(key==='approvals'){rows[0].status=0;totals={total:1,pending:1,stalePending:0,averageTurnaroundHours:null};}
 if(key==='pastoral-follow-up')rows=rows.slice(1);
 if(key==='data-quality')rows=rows.slice(1);
 if(key==='meeting-trends'){rows[0].status='Finalized';totals={eligible:4,present:3,rate:75,servantEligible:2,servantPresent:1,servantRate:50};}
 return {title:reportDefinitions.find(r=>r.key===key)!.name,period:'2026-09-18 – 2026-10-15 (القاهرة)',scope:'كل الأرقام مثال توضيحي؛ الإجماليات لكل النتائج المطابقة',columns,rows,totalCount:rows.length,page:1,pageSize:25,totals};
}
export async function manualFixtures(context:BrowserContext,app:string){
 await fixtures(context);
 await context.route('**/api/**',async route=>{
  const u=new URL(route.request().url()),path=u.pathname.toLowerCase().replace(/^.*\/api\//,'');
  const paged=(items:any[])=>({items,totalCount:items.length,page:1,pageSize:25});let body:any;
  if(route.request().method()==='OPTIONS')return route.fallback();
  if(path.includes('/photo') && route.request().method()==='GET')return route.fulfill({status:404,json:{message:'لا توجد صورة في المثال'}});
  if(path.startsWith('reports/suite/'))body=report(path.split('/')[2]);
  else if(path.startsWith('profile'))body={...person,role:app==='admin' ? 'Manager' : 'Secretary'};
  else if(path.includes('auth/'))body=session(app,{fullName:person.fullName,requiresPasswordChange:false,requiresProfileCompletion:false});
  else if(path==='studentcommands/import-preview')body=route.request().postDataJSON().rows.map((r:any)=>({row:r.row,candidates:r.row===2 ? [{id,name:'مينا سامح — تشابه للمراجعة'}] : [],error:r.student?.fullName==='صف يحتاج تصحيح' ? 'الهاتف غير صالح' : null}));
  else if(path==='studentcommands/bulk-import-v2')body={outcomes:[{row:2,studentId:id}]};
  else if(path.includes('academic-years'))body=['سنة اولى','سنة ثانية','سنة ثالثة','حديث التخرج','غير معروف'];
  else if(path.endsWith('/audit'))body=paged([{id,timestamp:'2026-10-14T08:00:00Z',actorName:person.fullName,reason:'تحديث بيانات التواصل',beforeJson:'{"address":"عنوان قديم"}',afterJson:'{"address":"عنوان تجريبي — شبرا البلد"}'}]);
  else if(path==='studentqueries/students-for-servant')body=paged(u.searchParams.get('segment')==='Unassigned' ? [{student:{id:second,fullName:'مرقس — مثال غير مسند',area:'بهتيم',college:'كلية الهندسة',academicYear:'سنة اولى'},segment:'Unassigned'}] : [student,other].map(student=>({student,segment:'Assigned'})));
  else if(path==='studentqueries/paged')body=paged([student,other]);
  else if(path==='studentcommands/archived')body=paged([{...other,isArchived:true}]);
  else if(path.startsWith('studentqueries/servant/'))body=[student,other];
  else if(path.startsWith('studentqueries/') && !path.endsWith('areas'))body=student;
  else if(path.startsWith('lookups/'))body=paged(path.endsWith('students') || path.endsWith('recipients') ? [student,other].map(s=>({id:s.id,name:s.fullName,phone:s.phone,secondaryPhone:s.secondaryPhone})) : [{id:owner,name:person.fullName},{id:second,name:'بطرس عادل'}]);
  else if(path.startsWith('activity-history/'))body=paged([call]);
  else if(path==='follow-up-tasks')body={...paged([{...task,status:u.searchParams.get('status')==='PendingReview' ? 'PendingReview' : 'Open',sourceType:u.searchParams.get('status')==='PendingReview' ? 'Legacy' : 'Manual'}]),dueStudents:0};
  else if(path==='meetings/settings')body={meetingDay:5,reportWeekStart:5,version};
  else if(path==='meetings/recurrence')body=[{date:'2026-10-16',session:null}];
  else if(path.includes('eligibility-candidates'))body=paged([{id,name:student.fullName},{id:second,name:other.fullName}]);
  else if(path.endsWith('/eligibility'))body=[{personId:id,name:student.fullName,kind:'Student',present:true,assignedServantId:owner}];
  else if(path.endsWith('/roster'))body=paged([student,other].map((s,i)=>({id:s.id,personId:s.id,name:s.fullName,present:i===0,assignedServantId:owner})));
  else if(path==='meetings')body=paged([meeting]);
  else if(path.startsWith('approvals/'))body=paged([{...request,kind:['addition','assignment','removal'].includes(path.split('/')[1]) ? path.split('/')[1] : 'addition'}]);
  else if(path.startsWith('notification/schedules'))body=[{id,daysOfWeek:'5',timeOfDay:'18:00:00',isActive:true}];
  else if(path==='operations/metrics')body={capturedAt:'2026-10-15T08:00:00Z',api:{errors:0},database:{slowQueries:1},workerLagSeconds:10,failedDeliveries:1,pendingMigrations:0};
  else if(path==='operations/deliveries')body=paged([{id,title:'تذكير بمتابعة الافتقاد — مثال',status:'Failed',availableAt:'2026-10-14T15:00:00Z',attempts:3,lastError:'تعذر إرسال الإشعار إلى الجهاز التجريبي'}]);
  else if(path.includes('reports/dashboard'))body={totalStudents:2,totalServants:2,totalCalls:3,totalVisits:1,callsThisWeek:3,visitsThisWeek:1,studentsNeedingFollowUp:1,studentsNotContacted:0,unassignedStudents:0,unassignedTasks:0,assignments:{'يوسف جرجس':2},recentActivities:[{description:'يوسف جرجس: تسجيل مكالمة',timestamp:'2026-10-12T16:00:00Z'}]};
  else if(path.includes('reports/servant/') && path.endsWith('performance'))body={servantId:owner,fullName:person.fullName,assignedStudentsCount:2,callsThisWeek:3,visitsThisWeek:1};
  else if(path==='reports/servant-stats')body=[{servantId:owner,assignedStudentsCount:2,lastCallDate:call.callDate,lastVisitDate:call.visitDate}];
  else if(path==='reports/servants-paged-with-stats' || path==='users/paged')body=paged([{...person,assignedStudentsCount:2,lastCallDate:call.callDate,lastVisitDate:call.visitDate}]);
  else if(path.includes('reports/students-by-'))body=paged([{groupKey:path.endsWith('area') ? 'شبرا البلد' : path.endsWith('academic-year') ? 'سنة ثالثة' : '1',count:2,studentIds:[id,second]}]);
  else if(path.startsWith('reports/'))body=paged([{...person,servantId:owner,assignedCount:2,assignedStudentsCount:2,callsInPeriod:3,visitsInPeriod:1,callsThisWeek:3,visitsThisWeek:1,studentId:second,fullName:path.includes('no-contact') ? other.fullName : person.fullName,servantName:person.fullName,lastContactDate:'2026-09-25T15:00:00Z'}]);
  else if(path.startsWith('users/'))body=person;
  else if(path==='users')body=[person];
  else if(path==='followup/servants')body=[person];
  else return route.fallback();
  if(route.request().method()!=='GET' && !path.startsWith('auth/') && !path.endsWith('import-preview') && path!=='studentcommands/bulk-import-v2')body={};
  await route.fulfill({json:body});
 });
}
