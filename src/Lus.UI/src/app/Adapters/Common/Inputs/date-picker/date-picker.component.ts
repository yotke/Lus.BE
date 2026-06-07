import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output, forwardRef, OnInit, OnDestroy } from '@angular/core';
import { NG_VALUE_ACCESSOR, ControlValueAccessor, FormGroup, FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatDatepickerInputEvent, MatDatepickerModule } from '@angular/material/datepicker';


@Component({
  selector: 'app-date-picker',
  templateUrl: './date-picker.component.html',
  styleUrls: ['./date-picker.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DatePickerComponent),
      multi: true
    }
  ]
})
export class DatePickerComponent implements ControlValueAccessor {
  @Input() placeholder = 'אנא בחר תאריך';
  @Input() title: string = 'מתאריך';
  @Input() showLabel: boolean = true;
  @Input() formGroup: FormGroup;
  @Input() formControlName: string | null = null;
  @Input() selectedValue: any | null = null;
  @Input() classes: string | null = null;
  @Input() min: any | null = null;
  @Input() max: any | null = null;

  // Add a private variable to hold the inner value
  innerValue: any;

  @Output() selectedValueChange = new EventEmitter<any>(); // Emit changes

  constructor(private datePipe: DatePipe) {

  }

  get combinedClasses(): string {
    // If classes are provided, combine them with default classes
    if (this.classes) {
      return `mat-mdc-form-field-infix ${this.classes}`;
    }
    return 'mat-mdc-form-field-infix';
  }
  formatDate(date: Date): string | null {
    return this.datePipe.transform(date, 'dd/MM/yyyy');
  }

  // Implement ControlValueAccessor methods

  writeValue(value: any) {
    this.innerValue = value;
  }

  registerOnChange(fn: any) {
    // This is where you'll pass the function to notify value changes
    this.onChange = fn;
  }

  registerOnTouched(fn: any) {
    // This is where you'll pass the function to notify touched events
    this.onTouched = fn;
  }

  // Create methods to handle changes and touched events

  private onChange: any = () => { };
  private onTouched: any = () => { };

  // Implement methods to set the value and call change and touched events

  setValue(value: any) {
    this.innerValue = value;

    this.selectedValueChange.emit(value);
    this.onChange(value);
    this.onTouched();
  }
  onDateChange(event: MatDatepickerInputEvent<Date>): void {
    this.setValue(event.value);
  }
  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup.get(this.formControlName) as FormControl;
    }
    return null;
  }
}
