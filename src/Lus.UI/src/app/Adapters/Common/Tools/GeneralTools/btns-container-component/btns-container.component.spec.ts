import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BtnsContainerComponent } from './btns-container.component';

describe('BtnsContainerComponent', () => {
  let component: BtnsContainerComponent;
  let fixture: ComponentFixture<BtnsContainerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BtnsContainerComponent]
    })
      .compileComponents();

    fixture = TestBed.createComponent(BtnsContainerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
