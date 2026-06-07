import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TriangleButtonComponent } from './triangle-button.component';

describe('TriangleButtonComponent', () => {
  let component: TriangleButtonComponent;
  let fixture: ComponentFixture<TriangleButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TriangleButtonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TriangleButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
