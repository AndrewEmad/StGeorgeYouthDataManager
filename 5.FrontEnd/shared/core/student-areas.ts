export const studentAreas = ["ابراهيم بك", "ابن الحكم", "ارض نوبار", "الاسكان الصناعي", "التعاون", "الخانكة", "الزيتون", "العبور", "الفرنواني", "القناطر الخيرية", "الكورنيش", "المرج", "المرج الجديدة", "المظلات", "المعادى الجديدة", "الملف", "المنشية", "المنشية الجديدة", "النزهه الجديده", "الوحدة", "ام بيومي", "باسوس", "بهتيم", "جامع ابو الهنا", "حدائق الاهرام", "شبرا البلد", "شبرا مصر", "عبود", "عزبة عثمان", "عزبه رستم", "قهوة شرف", "كلية الزراعة", "كوبرى عرابى", "مدينة السلام", "مساكن الصباغة", "مساكن الضباط", "مساكن حجازى", "مسطرد", "منطي", "اخرى"] as const;
function areaKey(value:string):string {return value.replace(/[\s\u200e\u200f\u061c\u200b\u064b-\u065f\u0670\u0640]/g,'').replace(/[أإآٱ]/g,'ا').replace(/[ىی]/g,'ي').replace(/ة/g,'ه');}
const aliases=new Map<string,string>(studentAreas.map(value=>[areaKey(value),value]));
for(const [alias,canonical] of [
  [
    "شبرا المحطة",
    "شبرا البلد"
  ],
  [
    "شبرا الخيمة",
    "شبرا البلد"
  ],
  [
    "شبرا الخيمه",
    "شبرا البلد"
  ],
  [
    "ارض نوبار شبرا الخيمه",
    "ارض نوبار"
  ],
  [
    "المساكن",
    "الوحدة"
  ],
  [
    "الملف شبرا الخيمه",
    "الملف"
  ],
  [
    "الوحدة عند المصانع",
    "الوحدة"
  ],
  [
    "خلف الكنيسة",
    "شبرا البلد"
  ],
  [
    "شارع زكريا",
    "شبرا البلد"
  ],
  [
    "منية السيرج",
    "اخرى"
  ]
] as const)aliases.set(areaKey(alias),canonical);
export function canonicalStudentArea(value:string|null|undefined):string {return !value?.trim() ? '' : aliases.get(areaKey(value)) ?? 'اخرى';}
export function studentAreaForEdit(value:string|null|undefined):string|null {return canonicalStudentArea(value) || null;}
