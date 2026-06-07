export class FilterEvent {
  dataFiltered: any[] | null;

  constructor(df?: any[]) {
    this.dataFiltered = df ?? null;
  }
}
