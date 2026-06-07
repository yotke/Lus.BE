import { Component, EventEmitter, forwardRef, Input, Output, ViewChild } from '@angular/core';
import { NG_VALUE_ACCESSOR, ControlValueAccessor, FormGroup, FormControl } from '@angular/forms';
import { MatDatepicker } from '@angular/material/datepicker';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-month-picker',
  templateUrl: './month-picker.component.html',
  styleUrls: ['./month-picker.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MonthPickerComponent),
      multi: true
    }
  ]
})
export class MonthPickerComponent implements ControlValueAccessor {
  @ViewChild('picker') datepicker: MatDatepicker<Date>;

  @Input() placeholder = 'אנא בחר חודש';
  @Input() title: string = 'מחודש';
  @Input() showLabel: boolean = true;
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = null;
  @Input() selectedValue: any | null = null;
  @Input() classes: string | null = null;

  @Output() selectedValueChange = new EventEmitter<any>(); // Emit changes
  innerValue: Date | string | null;

  onChange: (date: Date) => void;
  onTouched: () => void;


  constructor(private datePipe: DatePipe) { }

  get combinedClasses(): string {
    // If classes are provided, combine them with default classes
    if (this.classes) {
      return `mat-mdc-form-field-infix ${this.classes}`;
    }
    return 'mat-mdc-form-field-infix';
  }
  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup?.get(this.formControlName) as FormControl;
    }
    this.innerValue = this.selectedValue;
    return null;
  }
  writeValue(value: any): void {
    this.innerValue = value;
  }
  registerOnChange(fn: (date: Date) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  ShowOnlyMonth(value: any) {
    return this.datePipe.transform(value, 'MM/yyyy');
  }

  setValue(value: any) {
    this.innerValue = value;
    this.onChange(value);

    this.onTouched();
  }


  chosenMonthHandler(normalizedMonth: Date) {
    this.innerValue = normalizedMonth;
    if (this.onChange)
      this.onChange(normalizedMonth);
    this.selectedValueChange.emit(normalizedMonth)
    if (this.onTouched)
      this.onTouched();
    this.datepicker.close();
  }
  chosenYearHandler(normalizedYear: Date) {
    let ctrlValue: Date;
    if (typeof this.innerValue === 'string') {
      ctrlValue = new Date(this.innerValue);
    } else if (this.innerValue instanceof Date) {
      ctrlValue = this.innerValue;
    } else {
      ctrlValue = new Date();
    }

    ctrlValue.setFullYear(normalizedYear.getFullYear());
    this.innerValue = ctrlValue;
    if (this.onChange)
      this.onChange(this.innerValue);
  }
}