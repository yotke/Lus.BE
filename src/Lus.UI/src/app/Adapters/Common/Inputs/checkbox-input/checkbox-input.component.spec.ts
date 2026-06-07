import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CheckboxInputComponent } from './checkbox-input.component';
import { ReactiveFormsModule, FormsModule, FormControl, FormGroup } from '@angular/forms';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('CheckboxInputComponent', () => {
  let component: CheckboxInputComponent;
  let fixture: ComponentFixture<CheckboxInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CheckboxInputComponent],
      imports: [
        ReactiveFormsModule,
        FormsModule,
        MatCheckboxModule,
        NoopAnimationsModule
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(CheckboxInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should bind to a form control', () => {
    const control = new FormControl(true);
    component.formGroup = new FormGroup({ checkbox: control });
    component.formControlName = 'checkbox';
    fixture.detectChanges();

    const checkboxElement: HTMLInputElement = fixture.nativeElement.querySelector('mat-checkbox input');
    expect(checkboxElement.checked).toBe(true);

    control.setValue(false);
    fixture.detectChanges();
    expect(checkboxElement.checked).toBe(false);
  });

  it('should call onChange when checkbox value changes', () => {
    spyOn(component, 'onChange');

    const control = new FormControl(false);
    component.formGroup = new FormGroup({ checkbox: control });
    component.formControlName = 'checkbox';
    fixture.detectChanges();

    const checkboxElement: HTMLInputElement = fixture.nativeElement.querySelector('mat-checkbox input');
    checkboxElement.click();
    fixture.detectChanges();

    expect(component.onChange).toHaveBeenCalledWith(true);
  });

  it('should call onTouched when checkbox is touched', () => {
    spyOn(component, 'onTouched');

    const control = new FormControl(false);
    component.formGroup = new FormGroup({ checkbox: control });
    component.formControlName = 'checkbox';
    fixture.detectChanges();

    const checkboxElement: HTMLInputElement = fixture.nativeElement.querySelector('mat-checkbox input');
    checkboxElement.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(component.onTouched).toHaveBeenCalled();
  });

  it('should update the view when writeValue is called', () => {
    component.writeValue(true);
    fixture.detectChanges();

    const checkboxElement: HTMLInputElement = fixture.nativeElement.querySelector('mat-checkbox input');
    expect(checkboxElement.checked).toBe(true);
  });

  it('should display the correct placeholder', () => {
    component.placeholder = 'Test Placeholder';
    fixture.detectChanges();

    const checkboxLabelElement: HTMLElement = fixture.nativeElement.querySelector('mat-checkbox');
    expect(checkboxLabelElement.textContent?.trim()).toBe('Test Placeholder');
  });

  it('should display the correct title', () => {
    component.title = 'Test Title';
    fixture.detectChanges();

    const matFormFieldElement: HTMLElement = fixture.nativeElement.querySelector('mat-form-field');
    expect(matFormFieldElement.getAttribute('title')).toBe('Test Title');
  });
});
