import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MinusBtnComponent } from './minus-btn.component';

describe('MinusBtnComponent', () => {
  let component: MinusBtnComponent;
  let fixture: ComponentFixture<MinusBtnComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MinusBtnComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MinusBtnComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
