import { TestBed } from '@angular/core/testing';

import { GenricTableService } from './genric-table.service';

describe('GenricTableService', () => {
  let service: GenricTableService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(GenricTableService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
