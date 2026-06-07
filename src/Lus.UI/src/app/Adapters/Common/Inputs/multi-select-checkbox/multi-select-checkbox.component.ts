import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { FormGroup, FormControl, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-multi-select-checkbox',
  templateUrl: './multi-select-checkbox.component.html',
  styleUrls: ['./multi-select-checkbox.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MultiSelectCheckboxComponent),
      multi: true
    }
  ]
})
export class MultiSelectCheckboxComponent implements ControlValueAccessor {

  @Input() showLabel: boolean = true;
  @Input() listInput: { Id?: number | string | null | undefined; Name?: string | null;[key: string]: any }[] = [];
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

  @Output() selectedValuesChange = new EventEmitter<Array<string | number>>();
  @Output('OnChange') OnValueChangeCallback = new EventEmitter<Array<string | number>>();

  private onChange: any = () => { };
  private onTouched: any = () => { };

  constructor() { }

  // Angular Form Accessors
  get control(): FormControl | null {
    return this.formControlName ? this.formGroup?.get(this.formControlName) as FormControl : null;
  }

  writeValue(obj: any): void {
    this.selectedValues = Array.isArray(obj) ? obj : [];
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

  /**
   * Toggles selection only when the checkbox is clicked.
   */
  toggleSelection(itemId: any, event: Event) {
    const checkbox = event.target as HTMLInputElement;
    if (checkbox.checked) {
      this.selectedValues.push(itemId);
    } else {
      this.selectedValues = this.selectedValues.filter(id => id !== itemId);
    }
    this.propagateChanges();
  }

  // Emit and notify changes
  private propagateChanges(): void {
    this.onChange(this.selectedValues);
    this.onTouched();
    this.selectedValuesChange.emit(this.selectedValues);
    this.OnValueChangeCallback.emit(this.selectedValues);
  }
}
