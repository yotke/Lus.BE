import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-project-text-row',
  templateUrl: './project-text-row.component.html',
  styleUrls: ['./project-text-row.component.scss']
})
export class ProjectTextRowComponent {
  @Input() title: string;
  @Input('colMd') colNumber: number;
  @Input() projectField: string;

}
