import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RegularHeadlineComponent } from './regular-headline.component';

describe('RegularHeadlineComponent', () => {
  let component: RegularHeadlineComponent;
  let fixture: ComponentFixture<RegularHeadlineComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegularHeadlineComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegularHeadlineComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
