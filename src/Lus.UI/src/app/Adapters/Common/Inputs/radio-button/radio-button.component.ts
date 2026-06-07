import { Component, EventEmitter, Input, Output, forwardRef } from '@angular/core';
import { FormControl, FormGroup, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';

@Component({
  selector: 'app-radio-button',
  templateUrl: './radio-button.component.html',
  styleUrls: ['./radio-button.component.scss'],
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => RadioButtonComponent),
    multi: true
  }]
})
export class RadioButtonComponent implements ControlValueAccessor {
  @Input() uniqueId: any = 0;
  @Input() placeholder: string | undefined = 'בחר';
  @Input() showLabel: boolean = true;
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = 'empty';
  @Input() radioName1: string | null = 'כן';
  @Input() radioName2: string | null = 'לא';
  @Input() selectedValue: any | null = null;
  @Input() title: string = "";
  @Input() disabled: boolean = false;
  @Input() class: string = "regular";
  @Output() OnChange = new EventEmitter<boolean>();
  @Output() selectedValueChange = new EventEmitter<any>(); // Emit changes

  onInputChange(isStatic: boolean) {
    this.OnChange.emit(isStatic);


  }
  
  writeValue(value: any) {
    this.selectedValue = value;
    this.selectedValueChange.emit(value);
  }

  registerOnChange(fn: any) {
    this.onChange = fn;
  }

  registerOnTouched(fn: any) {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  private onChange: any = () => { };
  private onTouched: any = () => { };

  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup?.get(this.formControlName) as FormControl;
    }
    return null;
  }
}
