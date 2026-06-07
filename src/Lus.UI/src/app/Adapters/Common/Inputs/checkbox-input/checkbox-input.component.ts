import { Component, EventEmitter, Input, Output, forwardRef } from '@angular/core';
import { ControlValueAccessor, FormControl, FormGroup, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-checkbox-input',
  templateUrl: './checkbox-input.component.html',
  styleUrls: ['./checkbox-input.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CheckboxInputComponent),
      multi: true
    }
  ]
})
export class CheckboxInputComponent implements ControlValueAccessor {
  @Input() placeholder = 'placeholder';
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = 'empty';
  @Input() selectedValue: boolean | null = null; // assuming it's a checkbox, should be boolean
  @Input() title: string | null = null;

  @Output() valueChange = new EventEmitter<boolean>(); 

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

  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup?.get(this.formControlName) as FormControl;
    }
    return null;
  }

  emitValueChange(value: boolean) {
    this.valueChange.emit(value);
    this.onChange(value);
  }
}
