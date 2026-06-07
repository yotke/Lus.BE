import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ProjectTime } from 'src/app/Infrastructure/Classes & Models/Classes/project-time';
import { ExtendedHttpClient } from 'src/app/Infrastructure/Classes & Models/customClasses/extendedHttpClient';
import { HttpRequestService } from '../httpRequestService';

@Injectable({
  providedIn: 'root'
})
export class ProjectTimeService {

  constructor(private http: HttpRequestService) { }


  public CreateNewProject(newProjectTimes: ProjectTime[]): Observable<ProjectTime[]> {
    return this.http.post<ProjectTime[]>('v1/ProjectsTimes/CreateTimes', newProjectTimes)

  }
  public ModifyProject(ModifiedProjectTimes: ProjectTime[]): Observable<ProjectTime[]> {
    return this.http.post<ProjectTime[]>('v1/ProjectsTimes/ModifyTimes', ModifiedProjectTimes)
  }
  public DeleteProjectTime(ProjectTimeId: number) {
    console.log('ProjectTimeId', ProjectTimeId);
    return this.http.post<boolean>(`v1/ProjectsTimes/Delete`, { id: ProjectTimeId, name: '' })
  }
}
