import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NextBtnComponent } from './next-btn.component';

describe('AddBtnComponent', () => {
  let component: NextBtnComponent;
  let fixture: ComponentFixture<NextBtnComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ NextBtnComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NextBtnComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
