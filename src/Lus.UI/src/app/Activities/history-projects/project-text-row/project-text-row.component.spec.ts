import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectTextRowComponent } from './project-text-row.component';

describe('ProjectTextRowComponent', () => {
  let component: ProjectTextRowComponent;
  let fixture: ComponentFixture<ProjectTextRowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ProjectTextRowComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProjectTextRowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
