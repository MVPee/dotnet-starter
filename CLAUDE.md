# CLAUDE.md

.NET 10 Clean Architecture starter: ASP.NET Core controllers, EF Core + PostgreSQL, xUnit. See README.md for deployment.

## Layers and allowed references

| Project | May reference | Contains |
|---|---|---|
| `Starter.Domain` | nothing (no NuGet, no framework deps) | Models (`User`), invariants, `Result<T>`, `Error`, `<Concept>Errors` |
| `Starter.Application` | Domain | Use cases, commands, ports (`I<Concept>Repository`, `IUnitOfWork`), filters, `PagedList<T>` |
| `Starter.Infrastructure` | Application, Domain | `AppDbContext`, `<Concept>Entity`, EF configurations, mappers, repositories, migrations |
| `Starter.Api` | Application, Domain, Infrastructure (DI registration only) | Controllers, DTOs, DTO mappers, HTTP error mapping, `Program.cs` |

`tests/Starter.ArchitectureTests` enforces these rules. Never add a reference that breaks them; fix the design instead.

Vocabulary: the Clean Architecture "Entity" is the Domain model (`User`). `UserEntity` is the EF persistence class and never leaves Infrastructure.

## Repositories and Unit of Work (critical rule, no exceptions)

- Every repository exposes **only** the reusable CRUD of `IRepository<TModel, TFilter>`: `GetByIdAsync`, `GetAllAsync(filter)` (paged), `GetCountAsync(filter)`, `AddBatchAsync`, `UpdateBatchAsync`, `DeleteBatchAsync`.
- `I<Concept>Repository` is an empty interface extending `IRepository<<Concept>, <Concept>Filter>`. Never add methods to it.
- `<Concept>Filter` is a record extending `PagedFilter` (page/pageSize are clamped). New query needs = new nullable filter properties, handled in the repository's `ApplyFilter`.
- Implementations inherit `Repository<TModel, TEntity, TFilter>` and only override `ApplyFilter`, `ToModel`, `ToEntity`. `<Concept>Entity` implements `IPersistenceEntity`.
- Repositories only **stage** changes. The use case calls `IUnitOfWork.SaveChangesAsync` once at the end: one `SaveChanges` = one transaction, all or nothing.
- Unique-index violations are mapped to domain errors in `Persistence/UniqueConstraintErrors.cs`; add an entry for each new unique index.
- Only use cases write: Api never injects `IUnitOfWork` or a repository.
- What fails the tests (and so CI):
  - `RepositoryRulesTests`: repositories inherit `Repository<,,>`, are sealed, only override `ApplyFilter`/`ToModel`/`ToEntity`, `I<Concept>Repository` declares nothing, only repositories and `UnitOfWork` take `AppDbContext`.
  - `UnitOfWorkRulesTests`: Api depends on neither `IUnitOfWork` nor a repository; in Application only `*UseCase` types depend on `IUnitOfWork`; a use case has at most one `SaveChangesAsync` call site; in Infrastructure only `UnitOfWork` calls EF Core writing members (`SaveChanges*`, `ExecuteUpdate/Delete`, `ExecuteSql*`, `BeginTransaction`, `GetDbConnection`), checked on compiled IL including delegates.
  - `UniqueConstraintMappingTests` (integration): every unique index and alternate key in the EF model has an entry in `UniqueConstraintErrors`.

## Conventions

- **Mapping**: DTO <-> Command / Model happens in Api (`<Concept>DtoMapper`). Entity <-> Model happens in Infrastructure (`<Concept>EntityMapper`). Application and Domain never see DTOs or EF entities.
- **Use cases** are concrete `sealed` classes (`<Action>UseCase` with `ExecuteAsync`), injected directly. No interface per use case.
- **Use case names** use CRUD verbs: `Get<Concept>ByIdUseCase`, `Get<Concepts>UseCase` (paged list), `Create<Concept>UseCase`, `Update<Concept>UseCase`, `Delete<Concept>UseCase`. Never `List...`.
- **Interfaces only for ports**: abstractions Application needs from the outside world (repositories, clocks via `TimeProvider`, external services). Defined in Application, implemented in Infrastructure.
- **Business failures return `Result<T>` / `Error`** (declared in `<Concept>Errors`), never exceptions. Exceptions are for bugs and infrastructure failures; the Api's global handler turns them into 500 ProblemDetails.
- **Every input is validated twice, from one rule.** The rule lives once in Domain as a pure function (`<Concept>/<Thing>Rule.IsValid`, e.g. `EmailAddressRule`, `PersonNameRule`). The Api DTO applies it through a custom attribute in `Api/Validation` (all field errors at once, 400 `ValidationProblemDetails`); the Domain factory (`User.Create`) applies it again and returns `Result<T>` with a `<Concept>Errors` validation error for non-HTTP callers. Lengths use the shared `<Concept>Constraints` constants on both sides. Never write a rule twice.
- **Character policy**: emails are ASCII-only; person names accept letters from any script plus space, hyphen, apostrophe and period, and are stored NFC-normalized.
- **Pure logic goes in Domain, organised by concept** (`Domain/Users/...`). Never create a `Helpers/`, `Utils/` or `Common/Extensions` dump.
- **Everything in Infrastructure is `internal`** except the DI registration extension.
- Folders by feature: `Application/Users/CreateUser/`, `Api/Users/`, `Infrastructure/Persistence/Users/`.
- Code style is enforced by `.editorconfig` with warnings as errors: file-scoped namespaces, braces always, no `this.`, `_camelCase` private fields, PascalCase constants, System usings first, namespace matches folder.
- No magic numbers (named constants, e.g. `UserConstraints`), descriptive lambda parameter names, no narrating comments.
- Exception: route templates stay inline in controller attributes (`[Route("api/users")]`, `[HttpGet("{id:guid}")]`), never extracted to constants.
- Migrations under `Persistence/Migrations` are generated: never hand-edit them except to fix a generated mistake.

## Adding an endpoint

1. Domain: model / behaviour / `<Concept>Errors`.
2. Application: `<Action>Command` + `<Action>UseCase` returning `Result<T>`, writing through repositories then `IUnitOfWork.SaveChangesAsync`; for a new concept, `<Concept>Filter` + empty `I<Concept>Repository`; register the use case in `ApplicationServiceRegistration`.
3. Infrastructure: `<Concept>Entity : IPersistenceEntity`, `<Concept>EntityConfiguration`, `<Concept>EntityMapper`, `<Concept>Repository : Repository<...>` (only `ApplyFilter`/`ToModel`/`ToEntity`), unique indexes in `UniqueConstraintErrors`, register in `InfrastructureServiceRegistration`, then add a migration.
4. Api: request/response DTOs, `<Concept>DtoMapper`, controller action mapping `Result` to HTTP via the error extensions.
5. Tests: unit tests (Domain + use case with an in-memory fake under `tests/Starter.UnitTests/Fakes`) and integration tests hitting the endpoint against Postgres.

## Commands

```bash
dotnet tool restore
dotnet build Starter.slnx
dotnet format Starter.slnx                     # fix style; CI runs it with --verify-no-changes
dotnet test tests/Starter.UnitTests
dotnet test tests/Starter.ArchitectureTests
dotnet test tests/Starter.IntegrationTests     # requires Docker (Testcontainers)
docker compose up                              # Postgres + API with hot reload on http://localhost:5080

dotnet ef migrations add <Name> \
  --project src/Starter.Infrastructure \
  --startup-project src/Starter.Infrastructure \
  --output-dir Persistence/Migrations

# CI fails if an entity/configuration changed without a migration:
dotnet ef migrations has-pending-model-changes \
  --project src/Starter.Infrastructure --startup-project src/Starter.Infrastructure
```

CI is split in `.github/workflows/` (lint, test, migrations, docker) sharing `.github/actions/setup-dotnet`. The SDK version lives in `global.json` and the Dockerfile `sdk:` tag: change both together.

Config: `ConnectionStrings__Database`, `Database__ApplyMigrationsOnStartup` (true only in Development). Health: `GET /health`. Scalar UI at `/scalar` outside Production.

Package versions live in `Directory.Packages.props` (central package management): add `<PackageVersion>` there, and `<PackageReference>` without a version in the csproj.

## Before finishing any task

Run `dotnet format Starter.slnx` and then **all** test projects (unit, architecture, integration). Do not report done with failing tests or format diffs.
