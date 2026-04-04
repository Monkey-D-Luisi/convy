---
name: api-endpoint
description: "Quick workflow for adding a single API endpoint. Use when implementing a specific REST endpoint with its command/query, handler, validator, and test."
---
# API Endpoint Workflow

## When to Use
- Adding a single REST endpoint to the Convy backend
- Lighter than `backend-feature` when Domain entities already exist

## Procedure

### Step 1: Define Command/Query
Create in `backend/src/Convy.Application/Features/{Feature}/Commands/` or `Queries/`:
```csharp
public record {Action}{Entity}Command(...) : IRequest<Result<{ReturnType}>>;
```

### Step 2: Create Validator
In the same folder:
```csharp
public class {Action}{Entity}CommandValidator : AbstractValidator<{Action}{Entity}Command>
{
    public {Action}{Entity}CommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
```

### Step 3: Create Handler
```csharp
public class {Action}{Entity}CommandHandler : IRequestHandler<{Action}{Entity}Command, Result<{ReturnType}>>
{
    // Inject repository, implement logic
}
```

### Step 4: Create/Update DTOs
In `Features/{Feature}/DTOs/` — immutable records.

### Step 5: Add Endpoint
In `backend/src/Convy.API/`:
```csharp
app.MapPost("/api/v1/{resource}", async (IMediator mediator, {Command} command) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
}).RequireAuthorization();
```

### Step 6: Test
- Unit test for handler in `Convy.Application.Tests`
- API test for endpoint in `Convy.API.Tests`

### Step 7: Verify
```bash
dotnet build backend/Convy.sln && dotnet test backend/
```
