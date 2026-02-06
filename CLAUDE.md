# CommonHall — Enterprise Intranet & Employee Communication Platform

## What Is This
A full-featured enterprise intranet platform inspired by Staffbase. Multi-channel internal communications (web, mobile, email, SMS, digital signage). AI-powered content assistance, search, and analytics. Built as a monorepo.

## Tech Stack
- **Frontend**: Next.js 14 (App Router), TypeScript, Tailwind CSS, shadcn/ui, TanStack Query
- **Backend**: .NET 10 Web API, C# 14, Entity Framework Core 10, PostgreSQL 16
- **Search**: Elasticsearch 8
- **Cache**: Redis 7
- **Auth**: ASP.NET Identity + JWT bearer tokens + refresh tokens
- **Real-time**: SignalR
- **AI**: Azure OpenAI / Anthropic Claude API
- **File Storage**: Local disk (dev), Azure Blob Storage (prod)
- **Email Delivery**: SMTP (dev), SendGrid (prod)

## Repository Structure
/apps/web          → Next.js frontend
/apps/api          → .NET 10 backend (solution: CommonHall.sln)
  /CommonHall.Api          → Controllers, middleware, DI, Program.cs
  /CommonHall.Application  → MediatR handlers, DTOs, validators, interfaces
  /CommonHall.Domain       → Entities, enums, value objects, base classes
  /CommonHall.Infrastructure → EF Core, repositories, services, migrations
/packages/shared-types   → Shared TS types
/packages/ui             → Shared React components
/infrastructure/docker   → Docker Compose

## Architecture Patterns

### Backend
- **Clean Architecture**: Domain (innermost) → Application → Infrastructure → Api (outermost)
- **CQRS via MediatR**: Commands mutate state, Queries read. Separate handler files.
  - File naming: CreateNewsArticleCommand.cs, GetNewsArticleBySlugQuery.cs
  - Handlers in same file as request class
- **Repository Pattern**: Generic IRepository<T> + specific repos when needed
- **Domain entities** are rich models with behavior, not anemic DTOs

### Frontend
- **Feature-based folders**: src/features/news/, src/features/pages/, etc.
- Each feature folder: components/, hooks/, api/, types/
- **API hooks**: One custom hook per endpoint using TanStack Query

## Coding Conventions

### C# / .NET
- PascalCase for public members, _camelCase for private fields
- Nullable reference types enabled everywhere
- sealed on classes that shouldn't be inherited
- Async methods suffixed with Async
- All entities inherit from BaseEntity (Id:Guid, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- Soft delete via ISoftDeletable (IsDeleted, DeletedAt, DeletedBy) — global query filter
- All API responses use envelope: ApiResponse<T> with { data, errors, meta }
- Pagination: cursor-based via PaginatedResult<T> with { items, nextCursor, hasMore }
- Use records for DTOs and commands/queries
- Validation via FluentValidation (validator per command)

### TypeScript / React
- camelCase for variables/functions, PascalCase for components/types
- Prefer interface for object shapes, type for unions/intersections
- No any — use unknown when type is truly unknown
- All API calls through typed hooks in src/features/*/api/
- Form state with react-hook-form + zod validation

### API Design
- RESTful: GET /api/v1/news, POST /api/v1/news, GET /api/v1/news/{slug}
- All endpoints require [Authorize] unless explicitly [AllowAnonymous]
- Consistent error responses: ProblemDetails (RFC 7807)
- API versioning via URL prefix (/api/v1/)

## Database Conventions
- Table names: PascalCase plural (Users, NewsArticles, Pages)
- GUID primary keys (sequential for index perf)
- JSON columns use JSONB (PostgreSQL)
- Indexes on: all foreign keys, Slug fields, Status fields, IsDeleted
- Timestamps: UTC DateTimeOffset everywhere

## Common Commands
# Infrastructure
make dev-infra          # docker compose up -d
make dev-api            # dotnet run --project apps/api/CommonHall.Api
make dev-web            # cd apps/web && pnpm dev

# Database
cd apps/api
dotnet ef migrations add MigrationName --project CommonHall.Infrastructure --startup-project CommonHall.Api
dotnet ef database update --project CommonHall.Infrastructure --startup-project CommonHall.Api

# Testing
dotnet test             # all backend tests
cd apps/web && pnpm test

## Current Status
Phase 1 — Foundation. Building core CMS, auth, and content management.
