import { SearchRequestFilteringParams } from "../SearchRequestFilteringParams/search-request-filtering-params";
import { GroupingOperations } from "../Types/grouping-operations.enums";

export class SearchRequestFiltering {
  PropertyName: string | null = null;
  GroupingOperation: number = GroupingOperations.Or;
  FilterParameters: SearchRequestFilteringParams[] | null = null;

  constructor(init?: Partial<SearchRequestFiltering>) {
    Object.assign(this, init);
  }
}
