import { Component, EventEmitter, Input, Output, forwardRef } from '@angular/core';
import { ControlValueAccessor, FormControl, FormGroup, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-textarea-input',
  templateUrl: './text-area-input.component.html',
  styleUrls: ['./text-area-input.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TextAreaInputComponent),
      multi: true
    }
  ]
})
export class TextAreaInputComponent {
  @Input() placeholder: string = 'הכנס טקסט חופשי';
  @Input() title: string | undefined = '';
  @Input() showLabel: boolean = true;
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = 'empty';
  @Input() selectedValue: any | null = null;


  @Output('OnChange') OnValueChangeCallback = new EventEmitter<any>();

  emitSelectedValueChange(value: any) {
    this.OnValueChangeCallback.emit(value);
  }

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
}