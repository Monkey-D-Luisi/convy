---
description: "Use when editing ASP.NET Core API layer code — controllers, endpoints, middleware, DI configuration, and request/response handling."
applyTo: "backend/src/Convy.API/**"
---
# API Layer Guidelines

## Endpoint Design
- RESTful routing: `api/v1/{resource}` with kebab-case for multi-word resources.
- Group endpoints by feature/resource.
- Use `[Authorize]` on all endpoints by default. Explicitly mark public endpoints with `[AllowAnonymous]`.

## Request/Response
- Accept and return JSON.
- Success responses: return DTO directly with appropriate HTTP status (200, 201, 204).
- Error responses: use `ProblemDetails` format.
- Never expose domain entities directly — always map to DTOs.

## Dependency Injection
- Register services in extension methods per feature: `services.AddHouseholdFeature()`.
- Register MediatR, FluentValidation, and EF Core in `Program.cs`.

## Middleware Order
1. Exception handling (global)
2. Authentication (Firebase JWT)
3. Authorization
4. CORS
5. Routing + Endpoints

## Health Checks
- `GET /health` — basic liveness
- `GET /health/ready` — includes DB connectivity check
