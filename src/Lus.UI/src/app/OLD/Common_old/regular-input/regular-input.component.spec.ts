import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegularInputComponent } from './regular-input.component';

describe('RegularInputComponent', () => {
  let component: RegularInputComponent;
  let fixture: ComponentFixture<RegularInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ RegularInputComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegularInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
