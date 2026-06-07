import { Injectable } from '@angular/core';
import { TableColumn } from '../../../../../Infastructure/Classes/interfaces/ITableColumn/itable-columns';
import { EntityBaseFieldsDictionary } from '../../../../../Infastructure/Classes/ClassesColumnsDictionaries/EntityBase/entityBaseColumnsDirctionary';

@Injectable({
  providedIn: 'root'
})
export class GenricTableService {

  constructor() { }

  createDispalyesdTableColumnsEntitys(displayedColumns: string[] = [], tableColumn: TableColumn[] = [], childTableColumn: TableColumn[] = []) {
    const combinedFieldsDictionary = [...EntityBaseFieldsDictionary, ...tableColumn];

    // Map displayedColumns to displayedTableColumns
    const displayedTableColumns = displayedColumns
      .filter(column => column !== 'actions') // Exclude 'actions' because it's not part of the dictionaries
      .map(column => {
        const mainColumn = combinedFieldsDictionary.find(item => item.field === column);
        if (mainColumn) return mainColumn;
        const childColumn = childTableColumn.find(item => column.includes(item.field));
        if (childColumn) {
          return { field: column, header: childColumn.header };
        }
        return undefined;
      })
      .filter(Boolean) as TableColumn[]; // Filter out undefined entries

    return displayedTableColumns;
  }
  createDispalyesdTableColumns(displayedColumns: string[] = [], tableColumn: TableColumn[]) {
    const combinedFieldsDictionary = [...tableColumn];

    // Map displayedColumns to displayedTableColumns
    let displayedTableColumns = displayedColumns
      .filter(column => column !== 'actions') // Exclude 'actions' because it's not part of the dictionaries
      .map(column => combinedFieldsDictionary.find(item => item.field === column))
      .filter(Boolean) as TableColumn[]; // Filter out undefined entries

    return displayedTableColumns;
  }

  flattenEntity(data: any[]): any[] {
    const flatten = (obj: any): any => {
      const result: any = {};
      for (const key in obj) {
        if (Array.isArray(obj[key])) {
          result[key] = obj[key].map((item: any) => flatten(item));
        } else if (typeof obj[key] === 'object' && obj[key] !== null) {
          const nested = flatten(obj[key]);
          for (const nestedKey in nested) {
            result[`${key}.${nestedKey}`] = nested[nestedKey];
          }
        } else {
          result[key] = obj[key];
        }
      }
      return result;
    };

    return data.map(item => flatten(item));
  }

}
