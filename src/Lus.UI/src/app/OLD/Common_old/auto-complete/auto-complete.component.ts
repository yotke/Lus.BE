import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { FormGroup, FormControl, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-auto-complete',
  templateUrl: './auto-complete.component.html',
  styleUrls: ['./auto-complete.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => AutoCompleteComponent),
      multi: true
    }
  ]
})
export class AutoCompleteComponent implements ControlValueAccessor {
  @Input() listInput: { Id: number; Name: string }[] = [];
  @Input() placeholder = 'placeholder';
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = null;
  @Input() selectedValue: any | null = null;

 
  @Output() selectedValueChange = new EventEmitter<any>(); // Emit changes
  @Output('OnChange') OnValueChangeCallback = new EventEmitter<any>(); // Emit changes

  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup?.get(this.formControlName) as FormControl;
    }
    return null
  }

  // Implement ControlValueAccessor methods
  writeValue(obj: any): void {
    this.selectedValue = obj;
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  // Create methods to handle changes and touched events
  private onChange: any = () => { };
  private onTouched: any = () => { };

  // Method to handle changes from the input and propagate to the form
  setValue(value: any) {

    this.selectedValue = value;
    this.onChange(value);
    this.onTouched();
  }

  constructor() { }

  // Emit the selectedValueChange event when the selectedValue changes
  emitSelectedValueChange(value: any) {
    this.selectedValue = value;
    this.selectedValueChange.emit(value);
    this.OnValueChangeCallback.emit(value);
  }
}