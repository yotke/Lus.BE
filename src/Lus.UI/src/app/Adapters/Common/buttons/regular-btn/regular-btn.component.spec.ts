import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegularBtnComponent } from './regular-btn.component';

describe('RegularBtnComponent', () => {
  let component: RegularBtnComponent;
  let fixture: ComponentFixture<RegularBtnComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegularBtnComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegularBtnComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
