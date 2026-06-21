# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

NSchema.SQLite is the SQLite provider for [NSchema](https://github.com/nschema-org/NSchema), a schema-migration framework. It plugs SQLite-specific implementations of NSchema's `ISchemaProvider` (introspection) and `ISqlGenerator` (DDL generation) into the host application via `NSchemaApplicationBuilder.UseCurrentSchemaSqlite(...)`.

Target framework: `net10.0`. C# `LangVersion=latest` with nullable reference types and `TreatWarningsAsErrors=true`.

## Commands

- Build: `dotnet build NSchema.SQLite.slnx`
- Test (all): `dotnet test NSchema.SQLite.slnx`
- Test (single): `dotnet test --filter "FullyQualifiedName~SqliteSqlGeneratorSnapshotTests.MethodName"`
- Pack (matches CI output): `dotnet pack src/NSchema.SQLite/NSchema.SQLite.csproj -c Release`

Unlike the Postgres/Aws providers, the tests need **no Docker** — SQLite is in-process, so the execution tests open a private temp-file or `:memory:` connection directly (`Microsoft.Data.Sqlite`).

CI/CD runs through an external orchestrator at `nschema-org/NSchema` (`build/build/NSchema.Build`) rather than raw `dotnet` commands — see `.github/workflows/cicd.yml`. The build pipeline expects `Build__ProjectFile=src/NSchema.SQLite/NSchema.SQLite.csproj`.

## Architecture

Two service registrations make up the entire public surface; everything else is `internal`:

- **`SqliteSchemaProvider`** (`Sql/SqliteSchemaProvider.cs`) — reads the live database and assembles an NSchema `DatabaseSchema`. Structure comes from `PRAGMA` (`table_xinfo`, `foreign_key_list`, `index_list`/`index_info`), but SQLite's PRAGMAs **do not expose constraint names** (PK/FK/UNIQUE/CHECK). Those — and CHECK expressions and stored-generated expressions — are recovered by parsing the original `CREATE TABLE` text stored verbatim in `sqlite_master.sql` with `SqliteCreateTableParser`. NSchema's diff compares constraints **by name** (see `PrimaryKey.Equals`/`ForeignKey.Equals` in Core), so recovering the author's names is what lets `apply` → `plan` round-trip without phantom drift. Everything is reported under a single schema named **`main`** (SQLite's primary database; `temp`/attached DBs are out of scope).
- **`SqliteSqlGenerator`** (`Sql/SqliteSqlGenerator.cs`) — implements `ISqlGenerator`, translating an NSchema `MigrationPlan` into SQLite DDL. Objects are qualified as `"main"."name"`, which is valid SQLite. Column types are emitted as NSchema's **canonical type string** (`type.ToString()`, e.g. `bigint`, `varchar(255)`); SQLite stores the declared name verbatim and the introspector parses it back with `SqlType.Parse`, so types round-trip exactly (no INTEGER-affinity collapse).

### What SQLite can't do, and how the generator responds

- **`NotSupportedException` (no SQLite equivalent):** schemas other than `main`, sequences, enums, domains, composite types, routines (functions/procedures), grants, materialized views, and triggers. NSchema models a trigger as `EXECUTE FUNCTION fn(...)`; SQLite has no functions, so its (otherwise rich) triggers can't be expressed in the model — they are out of scope rather than half-supported.
- **`NotSupportedException` (would need a table rebuild):** in-place column changes (type, nullability, default, generated) and adding/dropping a PK/FK/UNIQUE/CHECK on an *existing* table. SQLite's `ALTER TABLE` only does ADD/DROP/RENAME COLUMN and RENAME TABLE; everything else is the 12-step rebuild (create new → copy → drop → rename), which is deliberately **not** implemented yet. The messages say so.
- **No-op (cosmetic):** all `Set*Comment` actions emit no SQL — SQLite has no `COMMENT ON`. A consequence is that a desired schema carrying doc comments will show those comment changes as perpetually pending; this is documented, not a bug.

`NSchemaApplicationBuilderExtensions` uses C# 14 **extension blocks** (`extension(NSchemaApplicationBuilder builder) { ... }`) — not classic `this`-parameter extension methods. Editing this file requires `LangVersion=latest` / .NET 10 SDK.

Central package management is on (`Directory.Packages.props`); add versions there, not in csproj files.
