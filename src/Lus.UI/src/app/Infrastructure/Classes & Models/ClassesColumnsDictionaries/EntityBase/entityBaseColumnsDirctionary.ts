import { TableColumn } from "../../Interfaces/ITableColumn/itable-columns";

/**
 * Shared audit/soft-delete columns available on every entity. Merged with an
 * entity-specific dictionary by GenricTableService.createDispalyesdTableColumnsEntitys.
 */
export const EntityBaseFieldsDictionary: TableColumn[] = [
  { field: 'Id', header: 'מזהה' },
  { field: 'CreatedOn', header: 'נוצר בתאריך' },
  { field: 'CreatedById', header: 'נוצר על ידי' },
  { field: 'UpdatedOn', header: 'עודכן בתאריך' },
  { field: 'UpdatedById', header: 'עודכן על ידי' },
  { field: 'DeletedOn', header: 'נמחק בתאריך' },
  { field: 'DeletedById', header: 'נמחק על ידי' },
  { field: 'IsDeleted', header: 'נמחק' },
  { field: 'Active', header: 'פעיל' }
];
