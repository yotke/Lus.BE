import { Component, OnInit, ChangeDetectorRef, Input, ViewChild, Output, EventEmitter, AfterViewInit } from '@angular/core';
import { MatMenuTrigger } from '@angular/material/menu';
import { FilterEvent } from './classes/filter-event';
import { Subscription } from "rxjs";
import { Emitters } from 'src/app/Infrastructure/Emitters/Emitters';

@Component({
  selector: 'app-filtering',
  templateUrl: './filtering.component.html',
  styleUrls: ['./filtering.component.scss']
})
export class FilteringComponent implements OnInit, AfterViewInit {
  @ViewChild('mouseOutMenuTrigger') menuNameCommon: MatMenuTrigger;
  @Output('filterDataFunction') filterDataEvent: EventEmitter<FilterEvent> = new EventEmitter<FilterEvent>();
  trigger: MatMenuTrigger;
  @Input('isFilterShow') isFilterShow: boolean = true;
  @Input('convertedValues') convertedValues: any;
  @Input('nameOfColumn') nameOfColumn: string;
  @Input('alternativeNameOfColumn') alternativeNameOfColumn: string | null = null;
  @Input('listOfData') listOfData: any[];
  filterMenuSubscription: Subscription
  reverseSorting: boolean;
  originalData: any[];
  menuIsOpen: boolean = false;

  constructor(private ref: ChangeDetectorRef) {
    this.filterMenuSubscription = Emitters.FilterMenuEmitter.subscribe(() => {
      if (this.menuIsOpen) {
        this.menuNameCommon.closeMenu();
      }
    });
  }

  ngOnDestroy() {
    this.filterMenuSubscription?.unsubscribe();
  }

  menuOpened() {
    Emitters.FilterMenuEmitter.emit();
    this.menuIsOpen = true;
  }

  menuClosed() {
    this.menuIsOpen = false;
  }

  sortingByColumn() {
    const names = this.nameOfColumn.split('.');
    if (names.length > 1) {
      this.listOfData?.sort((tv1, tv2) => {
        if (tv1[names[0]][names[1]]?.toString().trim() == tv2[names[0]][names[1]]?.toString().trim()) return 0;
        if (this.listOfData) {
          return (tv1[names[0]][names[1]]?.toString().trim() > tv2[names[0]][names[1]]?.toString().trim()) ? 1 : -1;
        } else {
          return (tv1[names[0]][names[1]]?.toString().trim() > tv2[names[0]][names[1]]?.toString().trim()) ? -1 : 1;
        }
      });
    } else {
      const sortedValues = this.listOfData.filter(tv => tv[this.nameOfColumn]);
      sortedValues.sort((tv1, tv2) => {
        if (this.isValidDate(tv1[this.nameOfColumn])) {
          if (this.reverseSorting) {
            return new Date(tv1[this.nameOfColumn]).getTime() - new Date(tv2[this.nameOfColumn]).getTime();
          } else {
            return new Date(tv2[this.nameOfColumn]).getTime() - new Date(tv1[this.nameOfColumn]).getTime();
          }
        } else {
          if (tv1[this.nameOfColumn].toString().trim() == tv2[this.nameOfColumn].toString().trim()) return 0;
          if (this.reverseSorting) {
            return (tv1[this.nameOfColumn].toString().trim() > tv2[this.nameOfColumn].toString().trim()) ? 1 : -1;
          } else {
            return (tv1[this.nameOfColumn].toString().trim() > tv2[this.nameOfColumn].toString().trim()) ? -1 : 1;
          }
        }
      });

      this.listOfData = sortedValues.concat(this.listOfData.filter(tv => !tv[this.nameOfColumn]));
    }

    this.filterDataEvent.emit(new FilterEvent(this.listOfData));

    this.reverseSorting = !this.reverseSorting;
  }

  isValidDate(value: any) {
    const dateWrapper = new Date(value);
    return !isNaN(dateWrapper.getDate());
  }

  closeOnMouseOut() {
    this.menuNameCommon.closeMenu();
  }

  filterValue(value: string) {
    if (!this.originalData || this.originalData.length == 0) {
      this.originalData = this.listOfData;
    }

    if (!value || value.length === 0) {
      this.filterDataEvent.emit(new FilterEvent(this.originalData));
      return;
    }

    const listOfDataFiltered = this.originalData.filter(t => {
      const names = this.nameOfColumn.split('.');
      const alternativeNames = this.alternativeNameOfColumn?.split('.');
      let results = null;
      if (names.length > 1) {
        if (this.convertedValues) {
          results = this.convertedValues[t[names[0]][names[1]]].toString().includes(value);
        } else {
          results = t[names[0]][names[1]]?.toString().includes(value);
        }
        if (!results && alternativeNames && alternativeNames.length > 1) {
          if (alternativeNames.length == 3) {
            if (this.convertedValues) {
              return this.convertedValues[t[names[0]][names[1]][names[2]]].toString().includes(value) || this.convertedValues[t[alternativeNames[0]][alternativeNames[1]][alternativeNames[2]]].toString().includes(value);
            } else {
              if (value.split(' ').length > 1) {
                return (t[names[0]][names[1]][names[2]]?.toString() + ' ' + t[alternativeNames[0]][alternativeNames[1]][alternativeNames[2]]?.toString()).includes(value);
              } else {
                return t[names[0]][names[1]][names[2]]?.toString().includes(value) || t[alternativeNames[0]][alternativeNames[1]][alternativeNames[2]]?.toString().includes(value);
              }
            }
          }
          else {
            if (this.convertedValues) {
              return this.convertedValues[t[names[0]][names[1]]].toString().includes(value) || this.convertedValues[t[alternativeNames[0]][alternativeNames[1]]].toString().includes(value);
            } else {
              if (value.split(' ').length > 1) {
                return (t[names[0]][names[1]]?.toString() + ' ' + t[alternativeNames[0]][alternativeNames[1]]?.toString()).includes(value);
              } else {
                return t[names[0]][names[1]]?.toString().includes(value) || t[alternativeNames[0]][alternativeNames[1]]?.toString().includes(value);
              }
            }
          }
        }

        return results;
      } else {
        if (this.convertedValues) {
          return this.convertedValues[t[this.nameOfColumn]].toString().includes(value);
        } else {
          return t[this.nameOfColumn]?.toString().includes(value) || (this.alternativeNameOfColumn ? t[this.alternativeNameOfColumn]?.toString().includes(value) : false);
        }
      }
    });

    this.filterDataEvent.emit(new FilterEvent(listOfDataFiltered));
  }

  clearFilters() {
    this.filterDataEvent.emit(new FilterEvent(this.originalData));
  }

  ngOnInit(): void {
  }

  ngOnChanges() {
  }

  ngAfterViewInit() {

  }
}
