import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule, ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { AutoCompleteComponent } from './auto-complete.component';
import { NgSelectModule } from '@ng-select/ng-select';

describe('AutoCompleteComponent', () => {
  let component: AutoCompleteComponent;
  let fixture: ComponentFixture<AutoCompleteComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AutoCompleteComponent],
      imports: [FormsModule, ReactiveFormsModule, NgSelectModule]
    })
      .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AutoCompleteComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should initialize with the correct placeholder', () => {
    component.placeholder = 'Test Placeholder';
    fixture.detectChanges();
    const ngSelect = fixture.debugElement.query(By.css('ng-select')).nativeElement;
    expect(ngSelect.getAttribute('placeholder')).toBe('Test Placeholder');
  });

  it('should update selectedValue when form control value changes', () => {
    const formGroup = new FormGroup({
      testControl: new FormControl()
    });
    component.formGroup = formGroup;
    component.formControlName = 'testControl';
    fixture.detectChanges();

    formGroup.controls['testControl'].setValue('newValue');
    fixture.detectChanges();

    expect(component.selectedValue).toBe('newValue');
  });

  it('should emit selectedValueChange when value changes', () => {
    spyOn(component.selectedValueChange, 'emit');
    component.setValue('newValue');
    fixture.detectChanges();

    expect(component.selectedValueChange.emit).toHaveBeenCalledWith('newValue');
  });

  it('should call registered onChange callback when value changes', () => {
    const onChangeSpy = jasmine.createSpy('onChange');
    component.registerOnChange(onChangeSpy);

    component.setValue('newValue');
    fixture.detectChanges();

    expect(onChangeSpy).toHaveBeenCalledWith('newValue');
  });

  it('should populate options correctly', () => {
    component.listInput = [
      { Id: 1, Name: 'Option 1' },
      { Id: 2, Name: 'Option 2' }
    ];
    fixture.detectChanges();

    const options = fixture.debugElement.queryAll(By.css('ng-option'));
    expect(options.length).toBe(2);
    expect(options[0].nativeElement.textContent.trim()).toBe('Option 1 - 1');
    expect(options[1].nativeElement.textContent.trim()).toBe('Option 2 - 2');
  });

  it('should handle form control when formControlName and formGroup are provided', () => {
    const formGroup = new FormGroup({
      testControl: new FormControl()
    });
    component.formGroup = formGroup;
    component.formControlName = 'testControl';
    fixture.detectChanges();

    const ngSelect = fixture.debugElement.query(By.css('ng-select')).nativeElement;
    expect(ngSelect).toBeTruthy();
  });

  it('should handle ngModel when formControlName is not provided', () => {
    component.selectedValue = 'initialValue';
    fixture.detectChanges();

    const ngSelect = fixture.debugElement.query(By.css('ng-select')).nativeElement;
    expect(ngSelect).toBeTruthy();
  });
});