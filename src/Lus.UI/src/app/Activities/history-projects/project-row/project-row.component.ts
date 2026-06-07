import { Component, Input } from '@angular/core';
import { ProjectTemplate } from 'src/app/Infrastructure/Classes & Models/Classes/project-template';

@Component({
  selector: 'app-project-row',
  templateUrl: './project-row.component.html',
  styleUrls: ['./project-row.component.scss']
})
export class ProjectRowComponent {
  @Input() project: ProjectTemplate;




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
