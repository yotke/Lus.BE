import { ProjectTemplate } from "./project-template";

export class ProjectTime {

    Id: number;
    ProjectName: string;
    ProjectNumber: number;
    sectionName: string;
    WorkDate: Date;
    ProjectTemplateId: number;
    WorkDescription: string;
    ProjectTemplate?: ProjectTemplate;
    TimesArray: TimesArray;
    JsonTime: string;


    DayIndicator?: string;

}
export class TimesArray {
    WorkingTimes: TimeRow[];
}
export class TimeRow {
    StartTime: string;
    EndTime: string;
    WorkLocation: string;
}