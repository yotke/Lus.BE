export class SearchRequestFraming {
  Skip: number = 0;
  Take: number = 10;

  constructor(init?: Partial<SearchRequestFraming>) {
    Object.assign(this, init);
  }
}
