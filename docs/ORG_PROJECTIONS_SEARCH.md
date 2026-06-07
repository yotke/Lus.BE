# Organizations, Projections & Search Engine

Three related concerns, ported from ArmyLuz, that work together for multi-tenant, efficient, filterable reads.

---

## 1. Organization (tenant) scoping

Every tenant-owned entity carries an `OrganizationId`. Scoping is enforced **at the handler layer** (not via EF global query filters), which keeps queries explicit and lets admin/cross-org flows opt out.

### Current organization resolution
- The current org id is stored per user in cache under the key `user_organization_id_{userId}` (EasyCaching).
- `ChangeCurrentOrganizationCommand` updates it; `GetCurrentOrganizationQuery` reads it.
- Auth cookie/claims also carry `organization_id` / `org_string_id` for fast access via `IUserAccessor`/`IProjectUser`.

### Pattern in handlers
```csharp
var userId = userAccessor.ProjectUser.Id;
var organizationId = await cache.GetAsync<int>($"user_organization_id_{userId}", ct);
// ...filter the query by organizationId...
```

Existing org flows already present in Luz: `GetOrganization(s)`, `GetCurrentOrganization`, `GetOrganizationsToUser`, `GetOrganizationContacts`, `CreateOrganization`, `ModifyOrganization`, `ChangeCurrentOrganization`, `AddOrganizationToUser`.

---

## 2. Projections (read models)

A **projection** is a lightweight POCO that selects only the fields a read needs, shaped at the DB level via `IQueryable.Select(...)`. It is **not** a denormalized table or event-sourced read model — it's an EF Core projection.

- **Where:** `Lus.Application/<Feature>/Projections/<Name>Projection.cs`
- **Includes `OrganizationId`** so it can be org-scoped.
- **Mapped to DTOs** with AutoMapper: `CreateMap<XProjection, XDto>()`.

Example:
```csharp
public class OrganizationProjection
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }
    public int? OrganizationId { get; set; }
}
```

### Retrievers
A **retriever** builds the base `IQueryable<TProjection>` from the `DbContext`:
- **Where:** `Lus.Infrastructure/Retrievers/<Name>Retriever.cs`
- Extends `Lus.FilterEngine.EntityFrameworkCore.DataRetriever<TProjection>`.
```csharp
public class OrganizationRetriever : DataRetriever<OrganizationProjection>
{
    public OrganizationRetriever(ApplicationContext context) : base(context) { }
    protected override IQueryable<OrganizationProjection> CreateRetrieveQuery(DbContext context) =>
        context.Set<Organization>().AsNoTracking().Select(o => new OrganizationProjection {
            Id = o.Id, Name = o.Name, Active = o.Active, OrganizationId = o.Id
        });
}
```

---

## 3. Search / Filter engine (`Lus.FilterEngine`)

A self-contained expression-tree engine (no external filter libraries) that turns a strongly-typed request into an EF Core `IQueryable` predicate + sort + paging.

### Request model
```csharp
SearchRequest<TDto> {
  ICollection<Filtering>? Filters;   // grouped AND/OR field filters
  ICollection<Sorting>?   Sorts;     // multi-column, nested paths
  Framing?                Framing;   // Skip / Take
  bool?                   SkipCount; // skip total count
  ICollection<string>?    Fields;    // projected fields
}
```
- `Filtering` → `PropertyName` + `GroupingOperation` (And/Or) + `FilterParameter[]`.
- `FilterParameter` → `Operation` (`Eq, Gt, Lt, Sw, Ct, In, IsNull, IsNotNull`), `Values`, `IsNegated`, `InnerPath` (nested), `AsString`.

### Response
`FramedResultDto<TDto> { IEnumerable<TDto> Items; int Count; }`

### Pipeline
`ISearchService<TDto>` → `SearchService<TDto, TProjection>`:
convert request (DTO→projection field names) → build predicate → select → sort → frame → execute via `IDataRetriever<TProjection>` → map projection → DTO.

### Org auto-scoping
Search query handlers **inject the current `OrganizationId` filter** if the caller didn't supply one:
```csharp
request.SearchRequest.Filters ??= new List<Filtering>();
if (!request.SearchRequest.Filters.Any(f => f.PropertyName == nameof(SearchXDto.OrganizationId)))
    request.SearchRequest.Filters.Add(new Filtering {
        GroupingOperation = BooleanOperation.And,
        PropertyName = nameof(SearchXDto.OrganizationId),
        FilterParameters = new[] { new FilterParameter {
            GroupingOperation = BooleanOperation.Or, Operation = FilterOperation.Eq,
            Values = new[] { organizationId.ToString() } } }
    });
return await searchService.SearchAsync(request.SearchRequest, new Sorting(nameof(SearchXDto.Id), true), ct);
```

---

## How to add search for a new entity (recipe)

1. **Projection** — `Lus.Application/<Feature>/Projections/<Name>Projection.cs` (include `OrganizationId`).
2. **Search DTO** — `Lus.Contracts/<Feature>/Search<Name>Dto.cs` (fields you allow filtering/sorting on).
3. **Retriever** — `Lus.Infrastructure/Retrievers/<Name>Retriever.cs : DataRetriever<<Name>Projection>`.
4. **Mapping** — `CreateMap<<Name>Projection, Search<Name>Dto>()` in the feature's profile.
5. **DI** — register `IDataRetriever<<Name>Projection> → <Name>Retriever` and `ISearchService<Search<Name>Dto> → SearchService<Search<Name>Dto, <Name>Projection>` (see `RetrieversExtensions`).
6. **Query + handler** — `<Name>SearchQuery(SearchRequest<Search<Name>Dto>)` + handler that auto-injects the org filter.
7. **Controller** — `POST /v1/<feature>/search` → `mediator.Send(new <Name>SearchQuery(request))`.

### Notes
- Drop ArmyLuz's `OracleBooleanSqlFixVisitor` — Luz uses MySQL (Pomelo); no Oracle boolean fix needed.
- Use `CaseInsensitiveSearchRequestPredicateBuilder<T>` if you want case-insensitive string matching.
