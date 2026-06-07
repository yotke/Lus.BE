export class SearchRequestSorting {
  SortBy: string = "Id";
  InverseOrder: boolean = true;
  InnerSortBy?: string = undefined;

  constructor(init?: Partial<SearchRequestSorting>) {
    Object.assign(this, init);
  }
}
