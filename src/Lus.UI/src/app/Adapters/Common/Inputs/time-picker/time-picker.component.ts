import { Component, Input, forwardRef } from '@angular/core';
import { FormGroup, FormControl, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-time-picker',
  templateUrl: './time-picker.component.html',
  styleUrls: ['./time-picker.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TimePickerComponent),
      multi: true
    }
  ]
})
export class TimePickerComponent implements ControlValueAccessor {
  @Input() placeholder: string | undefined = 'בחר שעה';
  @Input() title: string = 'משעה';
  @Input() showLabel: boolean = true;
  @Input() formGroup: FormGroup;
  @Input() formControlName: string | null = null;
  @Input() selectedValue: any | null = null;
  @Input() min: any | null = null;
  @Input() max: any | null = null;

  // Implement ControlValueAccessor methods
  writeValue(value: any) {
    this.selectedValue = value;
  }

  registerOnChange(fn: any) {
    this.onChange = fn;
  }

  registerOnTouched(fn: any) {
    this.onTouched = fn;
  }

  // Create methods to handle changes and touched events
  private onChange: any = () => { };
  private onTouched: any = () => { };

  constructor() { }

  // Method to handle changes from the input and propagate to the form
  setValue(value: any) {
    this.selectedValue = value;
    this.onChange(value);
    this.onTouched();
  }

  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup.get(this.formControlName) as FormControl;
    }
    return null;
  }
}