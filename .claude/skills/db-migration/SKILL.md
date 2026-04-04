---
name: db-migration
description: "Workflow for creating EF Core database migrations. Use when adding tables, columns, indexes, or modifying the database schema. Includes validation and rollback testing."
---
# Database Migration Workflow

## When to Use
- Adding a new table or modifying an existing one
- Adding indexes, constraints, or relationships
- Any schema change that requires a new EF Core migration

## Prerequisites
- Entity configuration exists in `backend/src/Convy.Infrastructure/Persistence/Configurations/`.
- Read `.github/instructions/ef-migrations.instructions.md` for rules.

## Procedure

### Step 1: Update Entity Configuration
In `backend/src/Convy.Infrastructure/Persistence/Configurations/{Entity}Configuration.cs`:
- Use Fluent API.
- Configure table name (`snake_case`), columns, max lengths, indexes, relationships.

### Step 2: Generate Migration
```bash
cd backend
dotnet ef migrations add <DescriptiveName> \
  --project src/Convy.Infrastructure \
  --startup-project src/Convy.API
```

### Step 3: Review Migration
1. Open the generated migration file.
2. Verify `Up()` creates/modifies schema correctly.
3. Verify `Down()` fully reverses all changes.
4. Check for proper index creation on foreign keys.

### Step 4: Apply and Test
```bash
# Apply migration
dotnet ef database update \
  --project src/Convy.Infrastructure \
  --startup-project src/Convy.API

# Test rollback
dotnet ef database update <PreviousMigrationName> \
  --project src/Convy.Infrastructure \
  --startup-project src/Convy.API

# Re-apply
dotnet ef database update \
  --project src/Convy.Infrastructure \
  --startup-project src/Convy.API
```

### Step 5: Generate SQL Script (for review)
```bash
dotnet ef migrations script \
  --project src/Convy.Infrastructure \
  --startup-project src/Convy.API
```

## Checklist
- [ ] Entity configuration uses Fluent API only
- [ ] Table and column names in `snake_case`
- [ ] `Down()` properly reverses `Up()`
- [ ] Foreign keys have indexes
- [ ] String columns have max length
- [ ] Nullable columns are intentional
- [ ] Migration applies and rolls back cleanly
