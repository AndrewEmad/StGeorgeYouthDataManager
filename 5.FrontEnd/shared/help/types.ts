export interface GuidePicture {file:string;caption:string;points:string[];}
export interface GuideTopic {
  id:string;title:string;group:string;intro:string;
  steps:string[];notes:string[];calculation?:string[];example?:string;
  pictures:GuidePicture[];path?:string;report?:string;attendanceDuty?:boolean;
}
export const picture=(file:string,caption:string,...points:string[]):GuidePicture=>({file,caption,points});
