import { ProjectTime } from "./project-time"

export class ProjectTemplate {
    Id: number;
    Name: string;
    ProjectNumber: number;
    SectionName: string;
    CurrentDate: Date;
    AccountNumber: string;
    ProjectSubject: string;
    WorkKindRate: string;
    WorkRate: string;
    WorkerName: string;
    ProjectLocation: string;
    ConstrctorName: string;
    StartContractDate: Date;
    EndContractDate: Date;
    WorkContractNumber: number;
    EmployeeSectionName: string;
    ConstrctorAddress: string;
    ConstrctorEntrepreneurNumber: string;
    OrganizationId: number;
    ConstrctorPhone: string;
    ConstrctorTitle: string;
    ProjectManager: string;
    ProjectTimes: ProjectTime[];
}
