import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MainHeadlineComponent } from './main-headline.component';

describe('MainHeadlineComponent', () => {
  let component: MainHeadlineComponent;
  let fixture: ComponentFixture<MainHeadlineComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MainHeadlineComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MainHeadlineComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
