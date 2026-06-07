# Search & Projection Engine

The UI builds a strongly-typed `SearchRequest` envelope that the backend search
engine translates into an EF Core query. This keeps filtering, sorting, paging
and **column projection** consistent across every list/table screen.

## Model

```
SearchRequest<T>
├── Filters : SearchRequestFiltering[]
│   ├── PropertyName        // entity property to filter on
│   ├── GroupingOperation   // how this block joins others (And/Or)
│   └── FilterParameters : SearchRequestFilteringParams[]
│       ├── Operation         // Eq | Gt | Lt | Sw | Ct | In | IsNull | IsNotNull
│       ├── GroupingOperation // how params within the block join (And/Or)
│       ├── Values            // string[] | number[]
│       ├── IsNegated
│       ├── InnerPath         // path into a nested/collection navigation
│       └── AsString          // compare a non-string column as text
├── Sorts : SearchRequestSorting[]
│   ├── SortBy
│   ├── InverseOrder
│   └── InnerSortBy
├── Framing : { Skip, Take }
├── Fields  : (keyof T)[] | null   // server-side projection (select only these)
└── SkipCount : boolean            // skip the COUNT query for cheap paging
```

Source files live under
`src/app/Infrastructure/Classes & Models/Filtering/SearchEntities/`.

### Enums

`Operations` (`Types/operations.enums.ts`):

| Value | Meaning |
|-------|---------|
| `Eq` (0)  | Equal |
| `Gt` (1)  | Greater than |
| `Lt` (2)  | Less than |
| `Sw` (3)  | Starts with |
| `Ct` (4)  | Contains |
| `In` (5)  | In set |
| `IsNull` (6) | Is null |
| `IsNotNull` (7) | Is not null |

`GroupingOperations`: `And = 0`, `Or = 1`.

## Building filters from a table

`GenericTableTypeUtilsService.buildFilterBlocks(rows, columnTerms, overrides)`
turns free-text per-column search terms into `SearchRequestFiltering[]`:

- **Type detection** — infers `number | date | boolean | string` from the data.
- **Numeric / date ranges** — `2024-01-01..2024-12-31` or `date:from=...;to=...`.
- **Operator overrides** — force an operator per column (`contains`, `gt`, `in`…).
- **Value mapping** — map display text to backend values (e.g. `"yes" -> 1`).
- **Projection target** — `property` override points a column at a different
  backend path, and `Fields` on `SearchRequest` limits the projected columns.

### Example

```ts
const filters = typeUtils.buildFilterBlocks(rows, {
  Name: 'roa',                 // Contains "roa"
  ProjectNumber: '>100',       // numeric > 100
  WorkDate: '2024-01-01..2024-06-30',
}, {
  ProjectNumber: { op: 'gt' },
});

const request = new SearchRequest<ProjectTemplate>({
  Filters: filters,
  Sorts: [new SearchRequestSorting({ SortBy: 'CreatedOn', InverseOrder: true })],
  Framing: new SearchRequestFraming({ Skip: 0, Take: 25 }),
  Fields: ['Id', 'Name', 'ProjectNumber', 'WorkDate'], // projection
});
```

The `Fields` projection means the API only selects and returns those columns,
reducing payload size and avoiding over-fetching nested graphs.
