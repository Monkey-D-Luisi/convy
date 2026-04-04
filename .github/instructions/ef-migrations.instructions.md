---
description: "Use when creating EF Core database migrations, modifying schema, adding indexes, or performing data transformations. Covers migration safety and rollback patterns."
---
# EF Core Migration Guidelines

## Creating a Migration

```bash
cd backend
dotnet ef migrations add <Name> --project src/Convy.Infrastructure --startup-project src/Convy.API
```

## Naming Convention
- Format: `DescriptiveAction` — EF Core adds the timestamp automatically.
- Examples: `AddListItemsTable`, `AddIndexOnListItemStatus`, `AddCompletedByToListItem`.

## Safety Rules
1. **Always implement `Down()` properly** — every migration must be fully reversible.
2. **Never drop columns** in the same migration/release that removes code using them. Use a two-phase approach.
3. **Never modify data in schema migrations** — use separate seed/data migration scripts.
4. **Test rollback** before committing: `dotnet ef database update <PreviousMigration>`.
5. **Add indexes explicitly** for foreign keys and frequently queried columns.

## Applying Migrations

```bash
# Apply all pending
dotnet ef database update --project src/Convy.Infrastructure --startup-project src/Convy.API

# Rollback to specific migration
dotnet ef database update <MigrationName> --project src/Convy.Infrastructure --startup-project src/Convy.API

# Generate SQL script (for review)
dotnet ef migrations script --project src/Convy.Infrastructure --startup-project src/Convy.API
```

## Review Checklist
- [ ] `Down()` reverses all `Up()` changes
- [ ] Foreign keys have indexes
- [ ] String columns have max length configured
- [ ] Nullable columns are intentional
- [ ] No data manipulation in migration
