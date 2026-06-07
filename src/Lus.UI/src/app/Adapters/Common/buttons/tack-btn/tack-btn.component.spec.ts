import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TackBtnComponent } from './tack-btn.component';

describe('TackBtnComponent', () => {
  let component: TackBtnComponent;
  let fixture: ComponentFixture<TackBtnComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TackBtnComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TackBtnComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
