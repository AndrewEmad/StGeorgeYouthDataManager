export interface ReportDefinition {key:string;name:string;description:string;group:string;icon:string;adminOnly?:boolean;}
export const reportDefinitions:ReportDefinition[]=[
  {key:'attendance',name:'حضور المخدومين والغياب المتتالي',description:'نسبة الحضور وآخر حضور والغياب المتتالي لكل مخدوم.',group:'الحضور',icon:'groups'},
  {key:'servant-attendance',name:'حضور الخدام',description:'حضور كل خادم في الاجتماعات التي كان ضمن قائمتها.',group:'الحضور',icon:'badge'},
  {key:'group-attendance',name:'حضور مجموعات الخدام',description:'حضور كل مجموعة بحسب خادمها وقت الاجتماع، مع تفاصيل المخدومين.',group:'الحضور',icon:'diversity_3',adminOnly:true},
  {key:'meeting-trends',name:'حضور الاجتماعات',description:'حضور المخدومين والخدام ونسبهم في كل اجتماع.',group:'الحضور',icon:'event_available'},
  {key:'attendance-history',name:'سجل حضور المخدومين',description:'تفاصيل الحضور المسجلة لكل اجتماع، بما فيها البيانات القديمة.',group:'الحضور',icon:'history'},
  {key:'servant-attendance-history',name:'سجل حضور الخدام',description:'تفاصيل حضور الخدام في الاجتماعات.',group:'الحضور',icon:'history'},
  {key:'servant-service-summary',name:'ملخص خدمة الخدام',description:'حضور الخادم وافتقاده ومجموعته ومتابعاته؛ دون درجات أو ترتيب تنافسي.',group:'خدمة الخدام والافتقاد',icon:'volunteer_activism',adminOnly:true},
  {key:'pastoral-follow-up',name:'مخدومون يحتاجون افتقادًا',description:'الغياب المتتالي أو غياب التواصل الناجح أو متابعة متأخرة، مع سبب المتابعة.',group:'خدمة الخدام والافتقاد',icon:'favorite',adminOnly:true},
  {key:'contact',name:'الافتقاد والتواصل',description:'محاولات الافتقاد والتواصل الناجح والمخدومون الذين لم تصل إليهم الخدمة.',group:'خدمة الخدام والافتقاد',icon:'phone_in_talk'},
  {key:'follow-up',name:'متابعة الافتقاد',description:'المكالمات والزيارات المطلوبة والمنجزة والمتأخرة والمسؤول عنها.',group:'خدمة الخدام والافتقاد',icon:'checklist'},
  {key:'workload-owners',name:'متابعة مجموعات الخدام',description:'حجم كل مجموعة وعدد المخدومين الذين تمت محاولة افتقادهم أو التواصل معهم بنجاح.',group:'خدمة الخدام والافتقاد',icon:'supervisor_account'},
  {key:'workload',name:'توزيع المخدومين',description:'الخادم المسؤول عن كل مخدوم والمخدومون غير الموزعين على خادم.',group:'بيانات المخدومين',icon:'account_tree'},
  {key:'data-quality',name:'بيانات تحتاج استكمالًا',description:'بيانات الاتصال والعنوان والمنطقة والدراسة والميلاد والتشابه المحتمل.',group:'بيانات المخدومين',icon:'fact_check'},
  {key:'approvals',name:'متابعة الطلبات',description:'الطلبات المنتظرة ونتائجها والوقت المستغرق للرد عليها.',group:'الطلبات',icon:'rule'}
];
export const pastoralReports=new Set(['servant-service-summary','group-attendance','pastoral-follow-up']);

// The servant workspace has a small, explicit catalog rather than inheriting
// management reports when a new report is added to the admin catalog.
export const servantReportDefinitions:ReportDefinition[]=[
  {key:'attendance',name:'حضور مجموعتي والغياب',description:'حضور مخدومي ونسب الحضور والغياب المتتالي في الفترة المختارة.',group:'مجموعتي',icon:'groups'},
  {key:'attendance-history',name:'سجل حضور مجموعتي',description:'تفاصيل حضور مخدومي في الاجتماعات.',group:'مجموعتي',icon:'history'},
  {key:'contact',name:'افتقادي لمجموعتي',description:'مكالماتي والزيارات التي سجلتها أو شاركت فيها لمخدومي، ومن يحتاج إلى تواصل.',group:'مجموعتي',icon:'phone_in_talk'},
  {key:'follow-up',name:'متابعاتي',description:'متابعات الافتقاد المسندة إليّ ومواعيدها وحالتها.',group:'متابعاتي وحضوري',icon:'checklist'},
  {key:'servant-attendance',name:'حضوري',description:'حضوري الشخصي ونسبته في الاجتماعات التي كنت ضمن قائمتها.',group:'متابعاتي وحضوري',icon:'badge'},
  {key:'servant-attendance-history',name:'سجل حضوري',description:'تفاصيل حضوري الشخصي في الاجتماعات.',group:'متابعاتي وحضوري',icon:'history'},
  {key:'data-quality',name:'بيانات مجموعتي الناقصة',description:'بيانات مخدومي التي تحتاج استكمالًا أو مراجعة.',group:'مجموعتي',icon:'fact_check'}
];
