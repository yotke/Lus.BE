import { Component, OnInit, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { MatStepper } from '@angular/material/stepper';
import { IdName } from 'src/app/Infrastructure/Classes & Models/Classes/id-name';
import { ProjectTemplate } from 'src/app/Infrastructure/Classes & Models/Classes/project-template';
import { ProjectTemplateService } from 'src/app/Infrastructure/Services/projectTemplateService/project-template.service';
@Component({
  selector: 'app-projects-manager',
  templateUrl: './projects-manager.component.html',
  styleUrls: ['./projects-manager.component.scss']
})
export class ProjectsManagerComponent implements OnInit, OnChanges {
  projectListIdName: IdName[] = []
  projectList: ProjectTemplate[] = []
  monthlyProjects: ProjectTemplate[] = [];
  monthlyProjectListIdName: IdName[] = []
  projectId: number | null;
  project: ProjectTemplate | undefined;
  isNewProject: boolean = false;
  CurrSystemDate: Date | null;
  hasCollision: boolean = false;
  @ViewChild('stepper') stepper: MatStepper;

  constructor(private projectSvc: ProjectTemplateService) {

  }

  ngOnInit() {
    this.projectSvc.currSystemDate$.subscribe(date => {
      this.CurrSystemDate = date;
      console.log(date);
      this.initProjectsAndProjectMonth(this.projectList);
    });
    this.projectSvc.GetAllProjects.subscribe((prjList: ProjectTemplate[]) => {
      this.projectSvc.projects$?.subscribe(projects => {
        this.initProjectsAndProjectMonth(projects);
      });
    })
  }
  ngOnChanges(changes: SimpleChanges) {

  }

  initProjectsAndProjectMonth(projects: ProjectTemplate[]) {
    this.projectList = projects;
    this.projectListIdName = projects.map(project => {
      return { Id: project.Id, Name: project.Name }
    });
    this.monthlyProjects = this.projectList.filter(p =>
      this.formatDateForComparison(new Date(p.CurrentDate)) == this.formatDateForComparison(this.CurrSystemDate ?? p.CurrentDate)
    );
    this.monthlyProjectListIdName = this.monthlyProjects.map(project => {
      return { Id: project.Id, Name: project.Name }
    });
  }
  formatDateForComparison(date: Date): string {
    const day = ('0' + date.getDate()).slice(-2); // Ensure day is always two digits
    const month = ('0' + (date.getMonth() + 1)).slice(-2); // Ensure month is always two digits
    const year = date.getFullYear();
    return `${year}-${month}-${day}`;
  }
  HideProjectComponent() {
    this.isNewProject = false;
  }

  OnProjectSelected(projectId: number | null) {
    console.log('OnProjectSelected => projectId',projectId);
    
    if (projectId) {
      this.project = this.projectList?.find(p => p.Id == projectId);
    }
    else {
      this.project = undefined;
    }

  }
  OnAdd() {
    this.isNewProject = true;
  }
  createNewProject() {
    this.project = undefined;
    this.projectId = null;
    this.isNewProject = true;
    this.stepper.reset();
  }
  onCollision(event: any) {
    console.log('onCollision', event);
    this.hasCollision = event;
  }
  SelectProject(project: ProjectTemplate) {
    this.project = project;

  }
}
