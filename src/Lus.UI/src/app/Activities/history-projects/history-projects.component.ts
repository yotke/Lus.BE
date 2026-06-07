import { Component, Input } from '@angular/core';
import { IdName } from 'src/app/Infrastructure/Classes & Models/Classes/id-name';
import { ProjectTemplate } from 'src/app/Infrastructure/Classes & Models/Classes/project-template';
import { ProjectTemplateService } from 'src/app/Infrastructure/Services/projectTemplateService/project-template.service';

@Component({
  selector: 'app-history-projects',
  templateUrl: './history-projects.component.html',
  styleUrls: ['./history-projects.component.scss']
})
export class HistoryProjectsComponent {
  @Input() isMonthly: boolean = false;
  idx: number | null = null;
  expandData: boolean = false;
  @Input() projectList: ProjectTemplate[]
  @Input() projectListIdName: IdName[];
  constructor(private projectSvc: ProjectTemplateService) { }
  ngOnInit() {
    if (!this.isMonthly)
      this.projectSvc.GetAllProjects.subscribe((prjList: ProjectTemplate[]) => {


        // console.log('project', prjList);
        this.projectList = prjList;
        this.projectListIdName = prjList.map(project => {
          return { Id: project.Id, Name: project.Name }
        });
      });
  }
  openTab(idx: number) {
    this.idx = idx;
    this.expandData = true;
  }
  closeTab() {
    this.idx = null;
    this.expandData = false;
  }
  changeCurrDate(CurrentDate: Date): string {
    const date = new Date(CurrentDate);
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    return `${month}/${date.getFullYear()}`;
  }
  changeDate(CurrentDate: Date): string {
    const date = new Date(CurrentDate);
    const day = date.getDate().toString().padStart(2, '0');  // This will pad single-digit days with a 0
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    return `${day}/${month}/${date.getFullYear()}`;
  }
}
