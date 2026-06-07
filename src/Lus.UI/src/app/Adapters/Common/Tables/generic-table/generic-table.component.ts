import { Component, Input, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { TableColumn } from 'src/app/Infrastructure/Classes & Models/Interfaces/ITableColumn/itable-columns';



@Component({
  selector: 'app-generic-table',
  templateUrl: './generic-table.component.html',
  styleUrl: './generic-table.component.scss'
})
export class GenericTableComponent implements OnInit {
  @Input() class: string = ''
  @Input() tableHeader: string = ''
  @Input() CallBackFunction: (id: any) => void = () => { };
  @Input('onPlay') onPlayCallBack: (element: any, event: Event) => void;
  @Input('onDelete') onDeleteCallBack: (element: any, event: Event) => void;
  @Input('onEdit') onEditCallBack: (element: any, event: Event) => void;
  @Input('onCopy') onCopyCallBack: (element: any, event: Event) => void;
  @Input('onPdf') onPdfCallBack: (element: any, event: Event) => void;
  @Input('onShowData') onShowDataCallBack: (element: any, event: Event) => void;
  /** Array of column definitions: { field, header } */
  @Input() columns: TableColumn[] = [];
  @Input() disabledAll: boolean = true;
  /** Array of data to display, each item should contain keys matching the `field`s of the columns */
  @Input() data: any[] = [];
  @Input() statusColumn: { header: string, field: string, data: number };

  /** The background color for the header toolbar */
  @Input() headerColor: string = '#607D8B'; // Default color

  /** Optionally control if the “Actions” column is shown or not */
  @Input() showActions: boolean = true;

  /** Data source for the Angular Material table */
  dataSource!: MatTableDataSource<any>;

  /** Computed array of column field names plus optional 'actions' column */
  displayedColumns: string[] = [];

  isBoolean(value: any): boolean {
    return typeof value === 'boolean';
  }
  ngOnInit() {

    this.dataSource = new MatTableDataSource(this.data);

    // Map columns and include 'actions' if required
    this.displayedColumns = this.columns.map(col => col.field);
    if (this.showActions) {
      this.displayedColumns.push('actions'); // Ensure actions column is included
    }
  }
}
