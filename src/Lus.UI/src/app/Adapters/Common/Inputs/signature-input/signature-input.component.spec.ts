import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SignatureInputComponent } from './signature-input.component';

describe('SignatureInputComponent', () => {
  let component: SignatureInputComponent;
  let fixture: ComponentFixture<SignatureInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SignatureInputComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SignatureInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
