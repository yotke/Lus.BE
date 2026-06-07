import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormControl, FormGroup } from '@angular/forms';
import { ProjectTemplate } from 'src/app/Infrastructure/Classes & Models/Classes/project-template';
import { ProjectTemplateService } from 'src/app/Infrastructure/Services/projectTemplateService/project-template.service';

@Component({
  selector: 'app-create-project',
  templateUrl: './create-project.component.html',
  styleUrls: ['./create-project.component.scss']
})
export class CreateProjectComponent implements OnChanges {
  @Input() CurrProject: ProjectTemplate | undefined = new ProjectTemplate();
  @Input() CurrDate: Date | null = null;
  @Output() OnReturnCallback = new EventEmitter<any>(); // Emit changes
  @Output() OnSaveCallback = new EventEmitter<any>(); // Emit changes

  CreateProjectForm: FormGroup;
  constructor(private formBuilder: FormBuilder, private projectSvc: ProjectTemplateService,) {
    this.initProjectForm();
  }
  ngAfterViewInit() {
    this.initProjectForm();
  }
  ngOnChanges(changes: SimpleChanges) {
    // Check if the property CurrProject has changed
    if (changes['CurrProject']) {
      const previousValue: ProjectTemplate | undefined = changes['CurrProject'].previousValue;
      const currentValue: ProjectTemplate | undefined = changes['CurrProject'].currentValue;

      if (previousValue !== currentValue) {
        this.handleCurrProjectChange(currentValue);
      }
    }
  }

  handleCurrProjectChange(newProject: ProjectTemplate | undefined) {
    // Your logic to handle the change goes here
    this.initProjectForm()
  }

  returnFromComponent() {

    this.OnReturnCallback.emit()
  }
  initProjectForm() {
    this.CreateProjectForm = this.formBuilder.group({});
    this.CreateProjectForm = this.formBuilder.group({
      Name: new FormControl(this.CurrProject?.Name ?? ''),
      ProjectNumber: new FormControl(this.CurrProject?.ProjectNumber ?? 0),
      SectionName: new FormControl(this.CurrProject?.SectionName ?? ''),
      CurrentDate: new FormControl(this.CurrProject?.CurrentDate ?? this.CurrDate ?? ''),

      ConstrctorTitle: new FormControl('אהובה אליה-הנדסת תנועה ותחבורה'),
      ConstrctorAddress: new FormControl('האורן 44 רמת ישי ת.ד. 1051'),
      ConstrctorPhone: new FormControl('054-9983894'),
      ConstrctorEntrepreneurNumber: new FormControl('057085250'),

      ProjectManager: new FormControl(this.CurrProject?.ProjectManager ?? ''),

      ProjectLocation: new FormControl(this.CurrProject?.ProjectLocation ?? 'עירית תל אביב'),
      ConstrctorName: new FormControl(this.CurrProject?.ConstrctorName ?? 'אהובה אליה'),
      StartContractDate: new FormControl(this.CurrProject?.StartContractDate ?? new Date('7/4/2022')),
      EndContractDate: new FormControl(this.CurrProject?.EndContractDate ?? new Date('7/3/2026')),
      WorkContractNumber: new FormControl(this.CurrProject?.WorkContractNumber ?? '202-22-746'),

      AccountNumber: new FormControl(this.CurrProject?.AccountNumber ?? ''),
      ProjectSubject: new FormControl(this.CurrProject?.ProjectSubject ?? ''),
      WorkerName: new FormControl(this.CurrProject?.WorkerName ?? 'אהובה אליה'),
      WorkKindRate: new FormControl(this.CurrProject?.WorkKindRate ?? ''),
      WorkRate: new FormControl(this.CurrProject?.WorkRate ?? '204.59'),
      EmployeeSectionName: new FormControl(this.CurrProject?.EmployeeSectionName ?? ''),
    });

  }
  onFormSubmit() {
    this.CurrProject = { Id: this.CurrProject?.Id, ...this.CreateProjectForm.value }
    if (this.CurrProject) {
      // console.log(this.CurrProject);

      if (!this.CurrProject.Id) {
        this.projectSvc.CreateNewProject(this.CurrProject).subscribe(savedProject => {
          this.CurrProject = savedProject;
          this.OnSaveCallback.emit(savedProject);
        });
      }
      else {
        this.projectSvc.ModifyProject(this.CurrProject).subscribe(savedProject => {
          this.CurrProject = savedProject;
          this.OnSaveCallback.emit(savedProject);
          // console.log(savedProject);
        });
      }
    }
  }
}
