import { TestBed } from '@angular/core/testing';

import { ProjectTimeService } from './project-time.service';

describe('ProjectTimeService', () => {
  let service: ProjectTimeService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ProjectTimeService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
