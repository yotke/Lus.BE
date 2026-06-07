import { Component, OnInit, Output, Input, ChangeDetectorRef, ChangeDetectionStrategy, EventEmitter, SimpleChanges } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ProjectTemplate } from 'src/app/Infrastructure/Classes & Models/Classes/project-template';
import { ProjectTime, TimeRow, TimesArray } from 'src/app/Infrastructure/Classes & Models/Classes/project-time';
import { ProjectTemplateService } from 'src/app/Infrastructure/Services/projectTemplateService/project-template.service';

@Component({
  selector: 'app-project-validator',
  templateUrl: './project-validator.component.html',
  styleUrls: ['./project-validator.component.scss'],
})
export class ProjectValidatorComponent implements OnInit {
  @Input() CurrProject: ProjectTemplate | undefined;
  MonthlyProjects: ProjectTemplate[];
  @Input() isExelStep: boolean = false;
  @Input() CurrDateRunning: Date;
  @Input() TimeArrayForm: AbstractControl<any, any> | null;
  AllProjectsTimesArrayForm: FormArray;
  AllProjectsTimesArrayTotMonthForm: FormArray | null;
  hasCollision: boolean = false;
  CurrSysDate: Date;
  daysInCurrMonth: Date[] = [];
  hasCollisionTot: boolean = false;
  private prevCurrProjectStr: string;

  @Output() hasCollisionEmiter = new EventEmitter<boolean>();
  private timeArrayFormChangesSub: Subscription;
  constructor(private projectTemplateSvc: ProjectTemplateService, private formBuilder: FormBuilder, private cd: ChangeDetectorRef) { }


  ngOnInit(): void {
    if (this.TimeArrayForm) {
      this.timeArrayFormChangesSub = this.TimeArrayForm.valueChanges.subscribe((changes) => {
        if (!this.isExelStep) {
          this.initProjectsTimesArrayForm();
          let res = this.IsTimeCollision();
        }
        else {
          this.getMonthlyProjectAndInitData();
        }
      });
    }
    this.projectTemplateSvc.currProjectChanged$.subscribe(res => {
      if (res) {
        this.getMonthlyProjectAndInitData();
      }
    })
    this.getMonthlyProjectAndInitData();
  }

  getMonthlyProjectAndInitData() {
    this.projectTemplateSvc.currSystemDate$.subscribe(currDate => {
      if (this.CurrProject || currDate) {
        this.CurrSysDate = currDate ?? new Date();
        this.projectTemplateSvc.GetMonthlyProjects(currDate ?? this.CurrProject?.CurrentDate ?? new Date())
          .subscribe(monthlyProjects => {
            if (!this.isExelStep) {
              this.MonthlyProjects = monthlyProjects.filter(monthlyProject => monthlyProject.Id !== this.CurrProject?.Id)
              this.initProjectsTimesArrayForm();
              let res = this.IsTimeCollision();
            }
            else {
              this.MonthlyProjects = monthlyProjects;
              if (this.isExelStep) {
                this.initMonthlyProjectsArrayCheck()
              }
            }
          })
      }
    });
  }
  ngAfterViewInit() { }
  ngOnChanges(changes: SimpleChanges) {
    if (this.MonthlyProjects) {
      this.initProjectsTimesArrayForm();
      let res = this.IsTimeCollision();

      if (this.isExelStep) {
        this.initMonthlyProjectsArrayCheck();
      }
    }

  }
  initMonthlyProjectsArrayCheck() {
    this.AllProjectsTimesArrayTotMonthForm = null;
    this.AllProjectsTimesArrayTotMonthForm = this.formBuilder.array([]);
    this.getDaysOfCurrentMonth();

    this.daysInCurrMonth.forEach(currDate => {
      this.initProjectsTimesArrayForm(currDate)
      if (this.IsTimeCollision()) {
        console.log('initMonthlyProjectsArrayCheck', this.AllProjectsTimesArrayForm);
        this.pushData()
      }
    });

    if (this.AllProjectsTimesArrayTotMonthForm.length > 0) {
      this.hasCollisionTot = true;
      this.hasCollisionEmiter.emit(true);
    }
    else {
      this.hasCollisionTot = false;

      this.hasCollisionEmiter.emit(false);
    }
  }
  pushData() {
    this.AllProjectsTimesArrayForm.controls.forEach(control => {
      this.AllProjectsTimesArrayTotMonthForm?.push(control);
    });
  }

  getDaysOfCurrentMonth() {
    this.daysInCurrMonth = [];
    const year = this.CurrSysDate.getFullYear();
    const month = this.CurrSysDate.getMonth();  // getMonth() is 0-indexed
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    for (let day = 1; day <= daysInMonth; day++) {
      this.daysInCurrMonth.push(new Date(year, month, day));
    }

  }

  get GetTimesValues(): FormArray {
    return (!this.isExelStep ? this.AllProjectsTimesArrayForm as FormArray : this.AllProjectsTimesArrayTotMonthForm as FormArray)
  }
  get groupedTimesByProject(): Record<string, Record<string, any[]>> {
    const grouped: Record<string, Record<string, any[]>> = {};
    if (this.isExelStep) {

    }
    this.GetTimesValues.value.forEach((item: any) => {
      const projectName = item.projectName;
      const workDate = item.WorkDate;

      if (!grouped[projectName]) {
        grouped[projectName] = {};
      }

      if (!grouped[projectName][workDate]) {
        grouped[projectName][workDate] = [];
      }

      grouped[projectName][workDate].push(item);
    });

    return grouped;
  }
  get keys(): any {
    return Object.keys;
  }
  private changeCurrDate(CurrentDate: Date): string {
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
  IsTimeCollision(): boolean {
    let formGroups;

    if (this.TimeArrayForm instanceof FormArray) {
      if (!this.isExelStep) {

        formGroups = [...this.TimeArrayForm.controls, ...this.AllProjectsTimesArrayForm.controls];
      }
      else {
        formGroups = [...this.AllProjectsTimesArrayForm.controls];
      }
    } else {
      formGroups = [...this.AllProjectsTimesArrayForm.controls];
    }

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
          this.hasCollision = true;

          return true;  // There's a collision
        }
      }
    }
    this.hasCollision = false;
    return false;  // No collisions found
  }

  parseTime(timeString: string): Date {
    const date = new Date();

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

  initProjectsTimesArrayForm(currDate: Date | null = null) {

    this.AllProjectsTimesArrayForm = this.formBuilder.array([])
    this.MonthlyProjects.forEach(monthlyProject => {
      monthlyProject.ProjectTimes.filter(pt => new Date(pt.WorkDate).getTime() == (!this.isExelStep ? new Date(this.CurrDateRunning)?.getTime() : currDate?.getTime())).map(projectTime => {
        let projectTimeArrayForm = this.initRowFormDateTime(projectTime, monthlyProject.Name);
        (projectTimeArrayForm.get('TimeFormArray') as FormArray).controls.forEach(control => {
          (this.AllProjectsTimesArrayForm as FormArray).push(control);
        });
      })
    });
  }
  initRowFormDateTime(projectTime: ProjectTime | null = null, projectName: string | null = null): FormGroup {
    return this.formBuilder.group({
      Id: new FormControl(projectTime?.Id ?? 0),
      WorkDate: new FormControl(projectTime?.WorkDate ?? ''),
      WorkDescription: new FormControl(projectTime?.WorkDescription ?? ''),
      TimeFormArray: this.initTimeArrayForm(projectTime?.TimesArray, projectName, projectTime?.WorkDate)
    });
  }
  initTimeArrayForm(timesArray: TimesArray | null = null, projectName: string | null = null, WorkDate: Date | null = null): FormArray {
    const formGroups = timesArray?.WorkingTimes?.map(timeRow => this.initRowFormTimeWithName(timeRow, projectName, WorkDate)) || [];
    return this.formBuilder.array(formGroups);

  }
  initRowFormTimeWithName(timeRow: TimeRow | null = null, projectName: string | null = null, WorkDate: Date | null = null) {
    return this.formBuilder.group({
      projectName: new FormControl(projectName ?? ''),
      StartTime: new FormControl(timeRow?.StartTime ?? ''),
      EndTime: new FormControl(timeRow?.EndTime ?? ''),
      WorkLocation: new FormControl(timeRow?.WorkLocation ?? ''),
      WorkDate: new FormControl(WorkDate ?? '')
    });
  }
}
