import { SearchRequestFiltering } from "../SearchRequestFiltering/search-request-filtering";
import { SearchRequestFraming } from "../SearchRequestFraming/search-request-framing";
import { SearchRequestSorting } from "../SearchRequestSorting/search-request-sorting";

/**
 * Generic search request envelope sent to the backend search/projection engine.
 *
 * - `Filters` describe per-property predicate groups.
 * - `Sorts` describe ordering.
 * - `Framing` describes paging (Skip/Take).
 * - `Fields` optionally projects only the requested columns (server-side projection).
 * - `SkipCount` skips the total-count query for cheaper paging when not needed.
 */
export class SearchRequest<T = any> {
  Filters: SearchRequestFiltering[] = [new SearchRequestFiltering()];
  Sorts: SearchRequestSorting[] = [new SearchRequestSorting()];
  Framing: SearchRequestFraming = new SearchRequestFraming();
  SkipCount?: boolean = false;
  Fields: (keyof T)[] | null = null;

  constructor(init?: Partial<SearchRequest<T>>) {
    Object.assign(this, init);
  }
}
