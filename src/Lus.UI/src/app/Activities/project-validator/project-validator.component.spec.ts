import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectValidatorComponent } from './project-validator.component';

describe('ProjectValidatorComponent', () => {
  let component: ProjectValidatorComponent;
  let fixture: ComponentFixture<ProjectValidatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ProjectValidatorComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProjectValidatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
