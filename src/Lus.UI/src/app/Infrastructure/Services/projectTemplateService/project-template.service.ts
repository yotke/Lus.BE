import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, Subject, tap } from 'rxjs';
import { ProjectTemplate } from 'src/app/Infrastructure/Classes & Models/Classes/project-template';
import { ExtendedHttpClient } from 'src/app/Infrastructure/Classes & Models/customClasses/extendedHttpClient';
import { API_BASE_URL } from 'src/app/app.module';
import { environment } from 'src/environments/environment';
import { HttpRequestService } from '../httpRequestService';

@Injectable({
  providedIn: 'root'
})
export class ProjectTemplateService {

  private BaseUrl = API_BASE_URL;
  CurrSystemDate: Date;
  private currSystemDateSubject = new BehaviorSubject<Date | null>(null);
  currSystemDate$ = this.currSystemDateSubject.asObservable();
  private currProjectChangedSubject = new Subject<boolean>();
  currProjectChanged$ = this.currProjectChangedSubject.asObservable();
  
  constructor(private http: HttpRequestService) { }
  projects$: Observable<ProjectTemplate[]>;
  updateSystemDate(newDate: Date) {
    this.currSystemDateSubject.next(newDate);
  }
  public get GetAllProjects(): Observable<ProjectTemplate[]> {
    return this.http.get<ProjectTemplate[]>('v1/ProjectsTemplates/GetProjects').pipe(
      tap(projects => {
        this.projects$ = new BehaviorSubject(projects);
      }))
  }
  notifyProjectChange(changedProject: boolean) {
    this.currProjectChangedSubject.next(changedProject);
  }
  public CreateNewProject(newProject: ProjectTemplate): Observable<ProjectTemplate> {
    let createProject = {
      ...newProject,
      CurrentDate: new Date(newProject.CurrentDate),
      EndContractDate: new Date(newProject.EndContractDate),
      StartContractDate: new Date(newProject.StartContractDate)
    }
    return this.http.post<ProjectTemplate>('v1/ProjectsTemplates/Create', createProject)
  }
  public ModifyProject(ModifiedProject: ProjectTemplate): Observable<ProjectTemplate> {
    let createProject = {
      ...ModifiedProject,
      CurrentDate: new Date(ModifiedProject.CurrentDate),
      EndContractDate: new Date(ModifiedProject.EndContractDate),
      StartContractDate: new Date(ModifiedProject.StartContractDate)
    }
    return this.http.post<ProjectTemplate>('v1/ProjectsTemplates/Modify', createProject)
  }


  public GetMonthlyProjects(projectCurrMonth: Date): Observable<ProjectTemplate[]> {
    return this.http.post<ProjectTemplate[]>(`v1/ProjectsTemplates/GetMonthlyProjects`, { CurrMonthDate: projectCurrMonth });
  }
}
