import { Component, EventEmitter, Input, Output, forwardRef } from '@angular/core';
import { ControlValueAccessor, FormControl, FormGroup, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-regular-input',
  templateUrl: './regular-input.component.html',
  styleUrls: ['./regular-input.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RegularInputComponent),
      multi: true
    }
  ]
})
export class RegularInputComponent implements ControlValueAccessor {
  @Input() placeholder: string = 'placeholder';
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = 'empty';
  @Input() selectedValue: any | null = null;
  @Input() title: string | null = null;
  @Input() showLabel: boolean = true;
  @Input() inputType: string = 'text';
  @Input() inputClass: string | null = null; // Custom class for styling
  @Input() inputStyle: { [key: string]: string } | null = null; // Custom inline styles
  @Output() selectedValueChange = new EventEmitter<any>(); // Emit changes
  @Output('OnChange') OnValueChangeCallback = new EventEmitter<any>(); // Emit changes

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
  emitSelectedValueChange(value: any) {
    this.selectedValueChange.emit(value);
    this.OnValueChangeCallback.emit(value);
  }
}
