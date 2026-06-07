import { GroupingOperations } from "../Types/grouping-operations.enums";
import { Operations } from "../Types/operations.enums";

export class SearchRequestFilteringParams {
  GroupingOperation: number = GroupingOperations.Or;
  Operation: number = Operations.Eq;
  IsNegated: boolean = false;
  Values: string[] | number[] | null = null;

  InnerPath?: string | null;
  AsString?: boolean | null;

  constructor(init?: Partial<SearchRequestFilteringParams>) {
    Object.assign(this, init);
  }
}
