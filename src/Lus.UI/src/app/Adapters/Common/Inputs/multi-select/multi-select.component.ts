import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { FormGroup, FormControl, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-multi-select',
  templateUrl: './multi-select.component.html',
  styleUrls: ['./multi-select.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MultiSelectComponent),
      multi: true
    }
  ]
})
export class MultiSelectComponent implements ControlValueAccessor {
  @Input() showLabel: boolean = true;
  @Input() listInput: { Id?: number | string | null | undefined; Name?: string | null; [key: string]: any }[] = [];
  @Input() uniqueId: any = 0;
  @Input() placeholder = 'placeholder';
  @Input() title = 'בחר פריטים';
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = null;

  @Input() selectedValues: Array<string | number> = [];

  @Input() class: string = 'regular';
  @Input() disabled: boolean = false;
  @Input() name: string | null = null;
  @Input() searchIcon: boolean = false;

  @Output() selectedValuesChange = new EventEmitter<Array<any>>();
  @Output('OnChange') OnValueChangeCallback = new EventEmitter<any>();

  private onChange: any = () => {};
  private onTouched: any = () => {};

  constructor() {}

  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup?.get(this.formControlName) as FormControl;
    }
    return null;
  }

  writeValue(obj: any): void {
    if (Array.isArray(obj)) {
      this.selectedValues = obj;
    } else {
      this.selectedValues = [];
    }
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

  onNgSelectChange(newSelection: any[]) {
    this.selectedValues = newSelection || [];
    this.onChange(this.selectedValues);
    this.onTouched();

    this.selectedValuesChange.emit(this.selectedValues);
    this.OnValueChangeCallback.emit(this.selectedValues);
  }
}
