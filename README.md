# ![NSchema](https://raw.githubusercontent.com/nschema-org/NSchema.Docs/main/assets/nschema-logo-horizontal.png)

[![NSchema.SQLite](https://github.com/nschema-org/NSchema.SQLite/actions/workflows/cicd.yml/badge.svg)](https://github.com/nschema-org/NSchema.SQLite/actions/workflows/cicd.yml)

# NSchema.SQLite

SQLite provider for [NSchema](https://github.com/nschema-org/NSchema), the declarative database schema migration tool for .NET. It plugs SQLite introspection and DDL generation into NSchema via [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/).

Most users should use the [NSchema CLI](https://github.com/nschema-org/NSchema), which already includes this provider. Add this package directly only when [embedding the engine](https://nschema.dev/library/embedding/) in your own application.

## Installation

```sh
dotnet add package NSchema.Core
dotnet add package NSchema.SQLite
```

## Scope

SQLite has a deliberately small surface, so this provider models what SQLite actually supports:

- **Supported:** tables, columns (with `DEFAULT` and stored generated columns), primary keys, foreign keys, unique constraints, check constraints, indexes, and views.
- **The schema is always `main`.** SQLite's primary database is `main`; declare objects as `main.<name>` in your DDL. (`temp` and `ATTACH`ed databases are out of scope.)
- **Native `ALTER TABLE` only.** Create/drop/rename tables and columns, and create/drop indexes/views, are applied directly. Operations SQLite cannot do in place — changing a column's type, nullability or default, or adding/dropping a constraint on an existing table — require a full table rebuild and currently raise a clear `NotSupportedException` rather than silently rebuilding.
- **Not supported (SQLite has no equivalent):** schemas other than `main`, sequences, enums, domains, composite types, stored functions/procedures, `GRANT`s, materialized views, and triggers (NSchema models triggers as calling a function, which SQLite has no concept of). These raise `NotSupportedException`.
- **Comments are not persisted.** SQLite has no `COMMENT ON`, so documentation comments are ignored when generating SQL.

Column types are emitted using NSchema's canonical type names (e.g. `bigint`, `varchar(255)`, `decimal(18,2)`); SQLite applies its normal type affinity and preserves the declared name, so a schema round-trips without phantom drift.

## Documentation

Full documentation lives at **[nschema.dev](https://nschema.dev)**:

- [Embedding the engine](https://nschema.dev/library/embedding/)

## License

See [LICENSE](LICENSE).
