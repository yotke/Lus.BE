import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExcelBtnComponent } from './excel-btn.component';

describe('ExcelBtnComponent', () => {
  let component: ExcelBtnComponent;
  let fixture: ComponentFixture<ExcelBtnComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ExcelBtnComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExcelBtnComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
