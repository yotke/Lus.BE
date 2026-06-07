import { Component, EventEmitter, Input, Output, SimpleChanges } from '@angular/core';
import { IdName } from '../../Infrastructure/Classes & Models/Classes/id-name';
import { FormArray, AbstractControl, FormBuilder, FormControl, FormGroup } from '@angular/forms';
import { animate, style, transition, trigger } from '@angular/animations';
import { DatePipe } from '@angular/common';
import { ProjectTemplate } from 'src/app/Infrastructure/Classes & Models/Classes/project-template';
import { ProjectTime, TimeRow, TimesArray } from 'src/app/Infrastructure/Classes & Models/Classes/project-time';
import { ProjectTimeService } from 'src/app/Infrastructure/Services/projectTimeService/project-time.service';
import { ProjectTemplateService } from 'src/app/Infrastructure/Services/projectTemplateService/project-template.service';


@Component({
  selector: 'app-data-loader',
  templateUrl: './data-loader.component.html',
  styleUrls: ['./data-loader.component.scss'],
  animations: [
    trigger('slideInOut', [
      transition(':enter', [
        style({ transform: 'translateY(-50%)' }),
        animate('1000ms ease', style({ transform: 'translateY(0%)' }))
      ]),
      transition(':leave', [
        animate('1000ms ease', style({ transform: 'translateY(-50%)' }))
      ])
    ])
  ]
})
export class DataLoaderComponent {
  DisplayedColumns = ['Id', 'filed1', 'field2', 'field3']

  @Input() CurrProject: ProjectTemplate | undefined;
  @Input() ProjectId: number | undefined;

  SectionList: IdName[] = []
  ProjectList: IdName[] = [];

  CurrProjectName: IdName | undefined;
  CurrSection: IdName | undefined;


  ProjectTimeForm: FormGroup;
  TimeFormArray: FormArray;
  DateTimeFormArray: FormArray;


  constructor(private projectTimeSvc: ProjectTimeService, private projectSvc: ProjectTemplateService, private formBuilder: FormBuilder, private datePipe: DatePipe) {
    this.TimeFormArray = this.formBuilder.array([]);
    this.DateTimeFormArray = this.formBuilder.array([]);
    this.ProjectTimeForm = this.formBuilder.group({
      sectionName: new FormControl(''),
      projectName: new FormControl(''),
      projectNumber: new FormControl(''),
      DateTimeFormArray: this.DateTimeFormArray
    });
  }

  ngOnInit() {
    this.ProjectList = [{ Id: 1, Name: this.CurrProject?.Name ?? '' }]
    this.SectionList = [{ Id: 1, Name: this.CurrProject?.EmployeeSectionName ?? '' }]
    this.CreateProjectForm();
    this.addRowFormDateTime();
    this.addTimeRowToDateTime();

  }
  ngOnChanges(changes: SimpleChanges) {
    // Check if the property CurrProject has changed
    if (changes['CurrProject']) {
      const previousValue: ProjectTemplate | undefined = changes['CurrProject'].previousValue;
      const currentValue: ProjectTemplate | undefined = changes['CurrProject'].currentValue;

      if (previousValue !== currentValue) {
        this.initProjectForm(this.CurrProject)
      }
    }
  }
  ngAfterViewInit() {
    this.initProjectForm(this.CurrProject)
  }

  onFormSubmit() {
    let projectTimes: ProjectTime[] = this.ProjectTimeForm.value.DateTimeFormArray.map((DateTimeRow: any) => {
      return {
        Id: DateTimeRow.Id,
        SectionName: this.ProjectTimeForm.get('sectionName')?.value?.Name,
        ProjectName: this.ProjectTimeForm.get('projectName')?.value?.Name,
        ProjectNumber: this.ProjectTimeForm.get('projectNumber')?.value?.Id,
        WorkDate: new Date(DateTimeRow.WorkDate),
        WorkDescription: DateTimeRow.WorkDescription,
        ProjectTemplateId: this.ProjectId,
        TimesArray: { WorkingTimes: DateTimeRow.TimeFormArray },
        JsonTime: JSON.stringify({ WorkingTimes: DateTimeRow.TimeFormArray })
      };
    });
    this.projectTimeSvc.ModifyProject(projectTimes).subscribe(projectTimesRes => {
      if (this.CurrProject) {
        this.CurrProject.ProjectTimes = projectTimesRes;
        this.initProjectForm(this.CurrProject);
        this.projectSvc.notifyProjectChange(true);
        this.projectSvc.GetAllProjects;
      }
    });

  }
  GetCurrDateTime(abstractControl: AbstractControl<any, any>): Date { return (abstractControl as FormGroup).get('WorkDate')?.value }
  GetTimeFormArray(abstractControl: AbstractControl<any, any>): AbstractControl<any, any> | null { return (abstractControl as FormGroup).get('TimeFormArray') }
  GetFormGroup(abstractControl: AbstractControl<any, any>): FormGroup { return abstractControl as FormGroup }
  GetFormArray(abstractControl: AbstractControl<any, any> | null): FormArray { return (abstractControl ?? new FormArray([])) as FormArray }

  GetStartTimeVal(timeGroup: AbstractControl<any, any>) {
    return this.GetFormGroup(timeGroup)?.get('StartTime')?.value ?? null
  }
  GetEndTimeVal(timeGroup: AbstractControl<any, any>) {
    let val = this.GetFormGroup(timeGroup)?.get('EndTime')?.value
    return (val == '' || val == null) ? null : val

  }

  get GetMinDateVal() { return this.getFirstDayOfMonth(this.CurrProject?.CurrentDate ?? new Date()).toISOString(); }
  get GetMaxDateVal() { return this.getLastDayOfMonth(this.CurrProject?.CurrentDate ?? new Date()).toISOString(); }

  getFirstDayOfMonth(date: Date): Date {
    const parsedDate = new Date(date);
    parsedDate.setDate(1);  // Set the day of the month to the 1st
    return parsedDate;
  }

  getLastDayOfMonth(date: Date): Date {
    const parsedDate = new Date(date);
    parsedDate.setMonth(parsedDate.getMonth() + 1); // Move to next month
    parsedDate.setDate(1);  // Set it to the first day of the next month
    parsedDate.setDate(parsedDate.getDate() - 1);  // Decrement by one day to get the last day of the original month
    return parsedDate;
  }

  IsTimeCollision(abstractFormArray: AbstractControl<any, any> | null): boolean {

    const formGroups = this.GetFormArray(abstractFormArray).controls;
    for (let i = 0; i < formGroups.length; i++) {
      for (let j = i + 1; j < formGroups.length; j++) {
        const group1 = formGroups[i] as FormGroup;
        const group2 = formGroups[j] as FormGroup;
        if (group1.controls['StartTime'].value == '' || group1.controls['EndTime'].value == '' || group2.controls['StartTime'].value == '' || group2.controls['EndTime'].value == '') {
          continue;
        }
        const startTime1 = this.parseTime(group1.controls['StartTime'].value);
        const endTime1 = this.parseTime(group1.controls['EndTime'].value);
        const startTime2 = this.parseTime(group2.controls['StartTime'].value);
        const endTime2 = this.parseTime(group2.controls['EndTime'].value);

        if (startTime1 < endTime2 && startTime2 < endTime1) {
          return true;  // There's a collision
        }
      }
    }

    return false;  // No collisions found
  }

  parseTime(timeString: string): Date {
    const date = new Date();
    // console.log(timeString);

    const timeParts = timeString.match(/(\d{1,2}):(\d{2})/);

    if (!timeParts) {
      throw new Error(`Invalid time format: '${timeString}'`);
    }

    const hours = +timeParts[1];
    const minutes = +timeParts[2];

    if (hours < 0 || hours > 24 || (hours === 24 && minutes !== 0)) {
      throw new Error('Hour out of bounds');
    }
    if (minutes < 0 || minutes > 59) {
      throw new Error('Minutes out of bounds');
    }

    date.setHours(hours, minutes, 0, 0);

    return date;
  }

  CreateProjectForm() {
    this.ProjectTimeForm = new FormGroup({
      projectName: new FormControl(this.CurrProject?.Name ?? ''),
      sectionName: new FormControl(this.CurrProject?.EmployeeSectionName ?? ''),
      projectNumber: new FormControl(this.CurrProject?.ProjectNumber ?? ''),
      DateTimeFormArray: this.DateTimeFormArray
    });
  }

  addRowFormDateTime() {
    this.DateTimeFormArray?.push(this.formBuilder.group({
      Id: new FormControl(0),
      WorkDate: new FormControl(''),
      WorkDescription: new FormControl(''), // Use array() instead of new FormArray([])
      TimeFormArray: this.formBuilder.array([]),  // Use array() instead of new FormArray([])
    }));
  }
  addTimeRowToDateTime(index: number = 0) {
    const timeArray = this.DateTimeFormArray.at(index).get('TimeFormArray') as FormArray;
    timeArray?.push(this.addRowFormTime());
    return this.TimeFormArray

  }
  addRowFormTime() {
    return this.formBuilder.group({
      StartTime: new FormControl(''),
      EndTime: new FormControl(''),
      WorkLocation: new FormControl('')
    });
  }

  RemoveDateTimeRow(idx: number) {
    let projectTimeId = this.DateTimeFormArray.at(idx).get('Id')?.value
    console.log(projectTimeId);

    if (projectTimeId) {

      this.projectTimeSvc.DeleteProjectTime(projectTimeId).subscribe(res => {
        this.DateTimeFormArray.removeAt(idx);
      })
    }
    else {
      this.DateTimeFormArray.removeAt(idx);
    }
  }

  /////// INIT VALUES 
  initProjectForm(currProject: ProjectTemplate | null = null) {
    this.ProjectTimeForm = new FormGroup({
      projectName: new FormControl(currProject?.Name ?? ''),
      sectionName: new FormControl(currProject?.EmployeeSectionName ?? ''),
      projectNumber: new FormControl(currProject?.ProjectNumber ?? ''),
      DateTimeFormArray: this.initDateTimeForm(currProject?.ProjectTimes)
    });
  }
  initDateTimeForm(ProjectTimes: ProjectTime[] | null = null): FormArray {
    const formGroups = ProjectTimes?.map(projectTime => this.initRowFormDateTime(projectTime)) || [this.initRowFormDateTime()];
    this.DateTimeFormArray = this.formBuilder.array(formGroups);
    return this.DateTimeFormArray;
  }
  initRowFormDateTime(projectTime: ProjectTime | null = null): FormGroup {
    return this.formBuilder.group({
      Id: new FormControl(projectTime?.Id ?? 0),
      WorkDate: new FormControl(projectTime?.WorkDate ?? ''),
      WorkDescription: new FormControl(projectTime?.WorkDescription ?? ''),
      TimeFormArray: this.initTimeArrayForm(projectTime?.TimesArray)
    });
  }
  initTimeArrayForm(timesArray: TimesArray | null = null): FormArray {
    const formGroups = timesArray?.WorkingTimes?.map(timeRow => this.initRowFormTime(timeRow)) || [this.initRowFormTime()];
    this.TimeFormArray = this.formBuilder.array(formGroups);
    return this.TimeFormArray;
  }
  initRowFormTime(timeRow: TimeRow | null = null) {
    return this.formBuilder.group({
      StartTime: new FormControl(timeRow?.StartTime ?? ''),
      EndTime: new FormControl(timeRow?.EndTime ?? ''),
      WorkLocation: new FormControl(timeRow?.WorkLocation ?? '')
    });
  }


}

