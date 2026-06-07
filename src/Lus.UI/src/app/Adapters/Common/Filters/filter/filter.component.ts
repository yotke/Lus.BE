import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { GenericFilterService } from '../../../../Infastructure/Services/Filters/generic-filters/generic-filter.service';


@Component({
  selector: 'app-filter',
  templateUrl: './filter.component.html',
  styleUrls: ['./filter.component.scss']
})
export class AppFilterComponent<T> implements OnInit, OnChanges {
  @Input() items: T[] = [];
  @Output() filteredChange = new EventEmitter<T[]>();

  // Example of internally managed filters: 
  // You could adapt these to different item properties.
  textFilter: string = '';
  statusFilter: string = 'all';
  startDate?: Date;
  endDate?: Date;

  // We'll store predicate references so we can remove/update them easily.
  private textPredicate: ((item: T) => boolean) | null = null;
  private statusPredicate: ((item: T) => boolean) | null = null;
  private datePredicate: ((item: T) => boolean) | null = null;

  constructor(private filterService: GenericFilterService<T>) {}

  ngOnInit(): void {
    // Initially, no filters are set. 
    // As the user interacts with the UI, we will add/update filters via the methods below.
    this.applyFilters();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ('items' in changes) {
      this.applyFilters();
    }
  }

  onTextFilterChange(value: string): void {
    this.textFilter = value;
    this.updateTextFilter();
    this.applyFilters();
  }

  onStatusChange(value: string): void {
    this.statusFilter = value;
    this.updateStatusFilter();
    this.applyFilters();
  }

  onStartDateChange(value: string): void {
    this.startDate = value ? new Date(value) : undefined;
    this.updateDateFilter();
    this.applyFilters();
  }

  onEndDateChange(value: string): void {
    this.endDate = value ? new Date(value) : undefined;
    this.updateDateFilter();
    this.applyFilters();
  }

  clearAllFilters(): void {
    this.textFilter = '';
    this.statusFilter = 'all';
    this.startDate = undefined;
    this.endDate = undefined;

    // Clear all filters from the service
    this.filterService.clearFilters();

    // Reset our predicate references
    this.textPredicate = null;
    this.statusPredicate = null;
    this.datePredicate = null;

    this.applyFilters();
  }

  private updateTextFilter(): void {
    // Remove old text predicate if exists
    if (this.textPredicate) {
      this.filterService.removeFilter(this.textPredicate);
      this.textPredicate = null;
    }

    if (this.textFilter.trim()) {
      this.textPredicate = (item: T) => {
        // Example assumes `item` has a `name` property. Adjust as needed.
        // If the item doesn't have `name`, adapt this logic or make it more generic.
        const nameVal = (item as any).name?.toString().toLowerCase();
        return nameVal ? nameVal.includes(this.textFilter.toLowerCase()) : false;
      };
      this.filterService.addFilter(this.textPredicate);
    }
  }

  private updateStatusFilter(): void {
    if (this.statusPredicate) {
      this.filterService.removeFilter(this.statusPredicate);
      this.statusPredicate = null;
    }

    if (this.statusFilter !== 'all') {
      this.statusPredicate = (item: T) => {
        // Example assumes `item` has a `status` property. Adjust as needed.
        return (item as any).status === this.statusFilter;
      };
      this.filterService.addFilter(this.statusPredicate);
    }
  }

  private updateDateFilter(): void {
    if (this.datePredicate) {
      this.filterService.removeFilter(this.datePredicate);
      this.datePredicate = null;
    }

    if (this.startDate || this.endDate) {
      this.datePredicate = (item: T) => {
        // Example assumes `item` has a `date` property. Adjust as needed.
        const d = (item as any).date ? new Date((item as any).date) : null;
        if (!d) return false;

        if (this.startDate && d < this.startDate) return false;
        if (this.endDate && d > this.endDate) return false;
        return true;
      };
      this.filterService.addFilter(this.datePredicate);
    }
  }

  private applyFilters(): void {
    // Get combined predicate and apply to items
    const predicate = this.filterService.getCombinedPredicate();
    const filtered = this.items.filter(predicate);
    this.filteredChange.emit(filtered);
  }
}
