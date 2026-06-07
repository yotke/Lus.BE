# Architecture

```
src/app/
├── Activities/            Feature screens (projects, history, excel export, ...)
├── Adapters/
│   ├── Common/            Reusable UI library
│   │   ├── buttons/         add/edit/delete/save/excel/... buttons
│   │   ├── Inputs/          auto-complete, date-picker, multi-select, table, ...
│   │   ├── Filters/         filter UI
│   │   ├── Tables/          generic-table + search/projection services
│   │   └── Tools/           layout & text helpers
│   ├── HomePageComponents/  header, home
│   ├── Interceptors/        auth (cookie), CSRF, error, blockUI
│   ├── Shared/              shared module, generic modal
│   └── loader/              overlay spinner
└── Infrastructure/
    ├── Classes & Models/
    │   ├── Classes/                 entities (EntityBase, ProjectTemplate, ...)
    │   ├── ClassesColumnsDictionaries/  table column dictionaries
    │   ├── Filtering/SearchEntities/    search request / filtering / sorting
    │   ├── Interfaces/              TableColumn, breadcrumb, ...
    │   └── Models/                  pagination
    ├── Emitters/             global event emitters
    ├── material/             centralized Angular Material module
    └── Services/             Auth, HTTP, project services
```

## Layers

- **Activities** — route-level feature components.
- **Adapters/Common** — presentation-only, reusable building blocks declared in
  `common-helpers.module.ts`.
- **Infrastructure** — framework-agnostic models, the search engine, and the
  services that talk to the API.

## Tables, search & projection

`GenericTableComponent` renders any entity list. Column metadata comes from a
per-entity dictionary merged with `EntityBaseFieldsDictionary` (the shared audit
columns) via `GenricTableService`. Free-text column search is converted into a
backend `SearchRequest` by `GenericTableTypeUtilsService` — see
[SEARCH_ENGINE.md](SEARCH_ENGINE.md).

## Authentication

Cookie + CSRF based — see [AUTH.md](AUTH.md).
