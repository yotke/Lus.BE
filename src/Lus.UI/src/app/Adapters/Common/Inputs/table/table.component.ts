import { Component, Input, AfterViewInit, Renderer2, Inject, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-table',
  templateUrl: './table.component.html',
  styleUrls: ['./table.component.scss']
})
export class TableComponent implements AfterViewInit, OnChanges {
  @Input() displayedColumns = ['Id', 'field1', 'field2'];
  @Input() isRouteTable: boolean = false;
  @Input() disabledAll: boolean = false;
  @Input() headLine: string = "טבלת נתונים"
  allInputIsvalid: boolean = true;
  newInputNeedValid: boolean = false;
  @Input() Data: any[] = [
    { Id: 1, Name: 'Tender A', StartDate: '2024-08-01', EndDate: '2024-08-31', Active: true },
    { Id: 2, Name: 'Tender B', StartDate: '2024-09-01', EndDate: '2024-09-30', Active: false },
    { Id: 3, Name: 'Tender C', StartDate: '2024-10-01', EndDate: '2024-10-31', Active: true }
  ];
  @Input() routePrefix: string = '';  // Optional prefix for the route

  constructor(private cdr: ChangeDetectorRef, private renderer: Renderer2, @Inject(DOCUMENT) private document: Document,
    private router: Router, private route: ActivatedRoute) { }

  ngAfterViewInit(): void {
    // Lifecycle hook to manipulate the DOM after the view has been initialized
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['Data']) {
      this.displayedColumns = this.getProcessFields();
    }
  }

  getProcessFields(): string[] {
    const fields: string[] = [];
    if (this.Data.length > 0) {
      Object.keys(this.Data[0]).forEach(field => {
        fields.push(field);
      });
    }
    return fields;
  }

  checkType(element: any, field: any): string {
    if (typeof element[field] === 'string') {
      return 'text';
    } else if (typeof element[field] === 'boolean') {
      return 'checkbox';
    } else {
      return typeof element[field];
    }
  }

  checkValidate(element: any, field: any): boolean {
    return true;
  }

  invalidInput(field: any, id: any): boolean {
    const name = this.setName(field, id);
    const inputEl = this.document.getElementById(name);
    if (inputEl) {
      if (inputEl.classList.contains('ng-invalid')) {
        this.allInputIsvalid = false;
        if (inputEl.classList.contains('ng-dirty')) {
          return true;
        }
      }
    }
    this.newInputNeedValid = false;
    return false;
  }

  setName(name: string, id: number): string {
    return `${name}_${id}`;
  }

  navigateToDetail(id: number): void {
    const routePath = this.routePrefix ? `${this.routePrefix}/${id}` : `${id}`;
    this.router.navigate([routePath], { relativeTo: this.route });
  }
}
