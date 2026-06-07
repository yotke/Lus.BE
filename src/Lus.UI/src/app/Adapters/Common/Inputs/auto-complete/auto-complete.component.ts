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
  @Input() showLabel: boolean = true;
  @Input() listInput: { Id?: number | string | null | undefined; Name?: string | null; [key: string]: any }[] = [];
  @Input() uniqueId: any = 0;
  @Input() placeholder = 'placeholder';
  @Input() title = 'בחר';
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = null;

  @Input() selectedValue: any | null = null;

  @Input() class: string = 'regular';
  @Input() disabled: boolean = false;
  @Input() name: string | null = null;
  @Input() searchIcon: boolean = false;

  @Output() selectedValueChange = new EventEmitter<any>(); // Single
  @Output('OnChange') OnValueChangeCallback = new EventEmitter<any>();

  private onChange: any = () => {};
  private onTouched: any = () => {};

  constructor() {}

  ngOnInit() {}

  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup?.get(this.formControlName) as FormControl;
    }
    return null;
  }

  writeValue(obj: any): void {
    this.selectedValue = obj;
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }
  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onNgSelectChange(newValue: any) {
    this.selectedValue = newValue;
    this.onChange(newValue);
    this.onTouched();
    this.selectedValueChange.emit(newValue);
    this.OnValueChangeCallback.emit(newValue);
  }
}
