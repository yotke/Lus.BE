import { Component, OnInit, ChangeDetectorRef, Input, Output, EventEmitter, AfterViewInit } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { PagerEvent } from './pager-event';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss']
})
export class PaginationComponent implements OnInit, AfterViewInit {

  @Input('length') dataLength: number;
  @Input('pageSizeOptions') pageSizes: string;
  @Output('displayFunction') displayChangeEvent: EventEmitter<PagerEvent> = new EventEmitter<PagerEvent>();

  pageOptions: Array<number>;
  currentPageSize: number;
  currentPage: number;
  ActivePages: Array<number>;
  allPages: number = 0;

  constructor(private ref: ChangeDetectorRef) { }

  ngOnInit(): void {
    this.dataLength = this.dataLength ?? 0;
    this.setUpPageSizes(this.pageSizes);
  }
  ngOnChanges(changeObj:any) {
    this.ref.detectChanges();
    this.onDataLengthChanges(changeObj.dataLength?.currentValue);
  }
  ngAfterViewInit() {
    this.displayChangeEvent.emit(new PagerEvent(this.dataViewSlic, this.currentPage, this.currentPageSize));
  }

  pageSizeEvent(event:any) {
    this.allPages = Math.floor(this.dataLength / this.currentPageSize) + 1;
    this.pagerEvent(1);
  }
  pagerEvent(toPage: number) {
    this.currentPage = toPage;
    this.reSetActivePages();

    this.displayChangeEvent.emit(new PagerEvent(this.dataViewSlic, this.currentPage, this.currentPageSize));
  }

  dataViewSlic(data?: Array<any>, page?: number, size?: number): Observable<any> | null {
    if (!data)
      return null;
    page = page ?? 1;
    size = size ?? 10;
    return new BehaviorSubject(
      data.slice(((page - 1) * size), ((page - 1) * size + size))
    );
  }

  get showPager(): boolean {
    return !((this.dataLength ?? 0) < (this.pageOptions ? this.pageOptions[0] : 0));
  }

  private reSetActivePages(): void {
    this.ActivePages = Array(isNaN(this.allPages) ? 0 : this.allPages).fill(
      0,
      this.currentPage - 4 > 0 ? (this.currentPage - 4) : 0,
      this.currentPage - 4 > 0 ? (this.currentPage + 3) : 7
    )
      .map((x, i) => (i + 1)).filter(n => n !== null);
  }
  private onDataLengthChanges(length?: number): void {
    this.dataLength = length ?? 0;
    this.allPages = Math.floor(this.dataLength / this.currentPageSize) + 1;
    this.reSetActivePages();
  }
  private setUpPageSizes(sizes: string): void {
    this.pageOptions = String(sizes ?? '10').split(',').map(v => Number(v)).sort();
    this.currentPageSize = this.pageOptions[0];
    this.currentPage = 1;
    this.allPages = Math.floor(this.dataLength / this.currentPageSize) + 1;
    this.reSetActivePages();
  }
}
