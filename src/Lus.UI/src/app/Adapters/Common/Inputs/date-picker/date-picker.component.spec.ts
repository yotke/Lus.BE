import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule, ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { DatePipe } from '@angular/common';
import { DatePickerComponent } from './date-picker.component';

describe('DatePickerComponent', () => {
  let component: DatePickerComponent;
  let fixture: ComponentFixture<DatePickerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [DatePickerComponent],
      imports: [
        FormsModule,
        ReactiveFormsModule,
        MatDatepickerModule,
        MatFormFieldModule,
        MatInputModule,
        BrowserAnimationsModule
      ],
      providers: [DatePipe]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DatePickerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with the correct placeholder', () => {
    component.placeholder = 'Test Placeholder';
    fixture.detectChanges();
    const inputElement = fixture.debugElement.query(By.css('input')).nativeElement;
    expect(inputElement.placeholder).toBe('Test Placeholder');
  });

  it('should update innerValue when form control value changes', () => {
    const formGroup = new FormGroup({
      testControl: new FormControl()
    });
    component.formGroup = formGroup;
    component.formControlName = 'testControl';
    fixture.detectChanges();

    formGroup.controls['testControl'].setValue('2021-12-31');
    fixture.detectChanges();

    expect(component.innerValue).toBe('2021-12-31');
  });

  it('should emit selectedValueChange when value changes', () => {
    spyOn(component.selectedValueChange, 'emit');
    component.setValue('2021-12-31');
    fixture.detectChanges();

    expect(component.selectedValueChange.emit).toHaveBeenCalledWith('2021-12-31');
  });

  it('should call registered onChange callback when value changes', () => {
    const onChangeSpy = jasmine.createSpy('onChange');
    component.registerOnChange(onChangeSpy);

    component.setValue('2021-12-31');
    fixture.detectChanges();

    expect(onChangeSpy).toHaveBeenCalledWith('2021-12-31');
  });

  it('should handle form control when formControlName and formGroup are provided', () => {
    const formGroup = new FormGroup({
      testControl: new FormControl()
    });
    component.formGroup = formGroup;
    component.formControlName = 'testControl';
    fixture.detectChanges();

    const inputElement = fixture.debugElement.query(By.css('input')).nativeElement;
    expect(inputElement).toBeTruthy();
  });

  it('should handle ngModel when formControlName is not provided', () => {
    component.innerValue = '2021-12-31';
    fixture.detectChanges();

    const inputElement = fixture.debugElement.query(By.css('input')).nativeElement;
    expect(inputElement.value).toBe('2021-12-31');
  });

  it('should format date correctly', () => {
    const formattedDate = component.formatDate(new Date('2021-12-31'));
    expect(formattedDate).toBe('31/12/2021');
  });

  it('should apply combined classes correctly', () => {
    component.classes = 'custom-class';
    fixture.detectChanges();

    const matFormFieldElement = fixture.debugElement.query(By.css('mat-form-field')).nativeElement;
    expect(matFormFieldElement.classList).toContain('custom-class');
    expect(matFormFieldElement.classList).toContain('mat-mdc-form-field-infix');
  });
});
