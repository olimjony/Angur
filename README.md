# Angur

Angur is my pet project for learning how to build backend services properly in .NET:
Clean Architecture, Domain-Driven Design, and tests that actually catch bugs.

The domain is a small core banking system, because that's what I work with day to day.
Banks are a good place to practice: the rules are strict, money must never appear out of
nowhere or disappear, and two requests hitting the same account at the same time is a real problem.

> Work in progress. The domain, application and persistence layers are done.
> The HTTP API is what I'm building next.

## What it can do so far

**Customers**
- register a customer (name, e-mail, date of birth; must be 18 or older)
- e-mail is unique and case-insensitive
- KYC: a pending customer can be verified or rejected (with a reason), only once

**Accounts**
- open an account in TJS, USD, EUR or RUB, only for a KYC-verified customer
- every account gets a 20-digit number from a database sequence
- deposit and withdraw (no overdraft, no mixing currencies)
- freeze with a reason (incoming money is still accepted, withdrawals are blocked), unfreeze
- close, only when the balance is zero

## Tech stack

- .NET 10, C# with nullable reference types and warnings treated as errors
- PostgreSQL 18 + EF Core 10 (Npgsql), snake_case naming
- xUnit v3 on Microsoft.Testing.Platform
- Testcontainers for integration tests against a real PostgreSQL
- NetArchTest for architecture rules
- Docker Compose for the local database

## Project structure

```
src/
  Angur.Domain           entities, value objects, business rules. No dependencies at all.
  Angur.Application      use cases (command handlers) and the ports they need
                         (repositories, unit of work, account number generator).
  Angur.Infrastructure   EF Core, PostgreSQL, migrations and the implementations of those ports.
  Angur.Api              ASP.NET Core host. Wires everything together.

tests/
  Angur.Domain.UnitTests                  business rules, no I/O
  Angur.Application.UnitTests             handlers with hand-written fakes
  Angur.Infrastructure.IntegrationTests   real PostgreSQL in Docker
  Angur.ArchitectureTests                 layer dependency rules
  Angur.Api.IntegrationTests              empty for now
```

Dependencies only point inwards: `Api -> Infrastructure -> Application -> Domain`.
The Domain project doesn't know EF Core or ASP.NET exist, and the architecture tests fail if that ever changes.

## Getting started

You'll need:
- [.NET SDK 10.0.400](https://dotnet.microsoft.com/download) or newer (pinned in `global.json`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/), running

```powershell
git clone https://github.com/olimjony/Angur.git
cd Angur

# restores dotnet-ef, which is installed as a local tool (see dotnet-tools.json)
dotnet tool restore

# start PostgreSQL on localhost:5432
docker compose up -d

# create the tables
dotnet ef database update --project src/Angur.Infrastructure --startup-project src/Angur.Api

dotnet build
```

The connection string lives in `src/Angur.Api/appsettings.Development.json`.
The password in there is for the local Docker database only. Nothing outside your machine uses it.

To stop the database: `docker compose down`. The data stays in a Docker volume.
To wipe it completely: `docker compose down -v`.

## Running the tests

```powershell
dotnet test
```

Docker has to be running. The integration tests start their own throwaway PostgreSQL container
(it doesn't touch the one from `docker compose`) and apply the real migrations to it.
The first run takes a bit longer because it pulls the image.

`dotnet test` currently ends with exit code 8. That comes from `Angur.Api.IntegrationTests`,
which has no tests yet, and it goes away once the API has some.

## Database migrations

After changing an entity configuration in `Angur.Infrastructure/Database/Configurations`:

```powershell
dotnet ef migrations add <DescriptiveName> --project src/Angur.Infrastructure --startup-project src/Angur.Api --output-dir Database/Migrations
dotnet ef database update --project src/Angur.Infrastructure --startup-project src/Angur.Api
```

A few rules I stick to:
- read the generated migration before applying it. EF sometimes drops and re-adds a column when you meant to rename it.
- never edit a migration that's already been applied anywhere. Add a new one instead.
- if you forget to add a migration, `MigrationTests` will tell you.

## Design decisions worth knowing

- **Business errors are values, bugs are exceptions.** Domain methods return `Result` / `Result<T>`
  with a `DomainError` ("insufficient funds", "account is frozen"). Exceptions are only for
  programmer mistakes such as passing `null`.
- **A failed operation changes nothing.** Every domain method checks all of its rules before touching
  state, and handlers only call `SaveChanges` on success.
- **Optimistic concurrency.** Customers and accounts use PostgreSQL's `xmin` as a concurrency token.
  If two requests load the same account and both try to withdraw, the second save fails instead
  of spending the money twice. There's an integration test for exactly that.
- **Uniqueness is enforced twice.** The handler checks that the e-mail is free, and a unique index
  catches the race where two requests pass that check at the same moment.
- **Money** is a value object (amount + currency), stored as `numeric(19,4)`. Never `float` or `double`.
- **IDs** are strongly typed (`CustomerId`, `AccountId`) and generated in the domain as UUID v7,
  so they're known before saving and sort by creation time.
- **Aggregates reference each other by ID only.** An `Account` stores a `CustomerId`, not a `Customer`.
  The foreign key still exists in the database.
- **Time comes from `TimeProvider`**, never from `DateTime.Now`, so tests can freeze the clock.
- **No MediatR, FluentAssertions or Moq.** The first two went commercial. The mediator is a few
  lines of my own code, assertions are plain xUnit, and test doubles are small hand-written fakes.

Account numbers look like `20206 840 000000000001`: a 5-digit balance account, the ISO 4217
numeric currency code (840 = USD), then 12 digits from a PostgreSQL sequence. It's simplified,
not a real national format.

## Roadmap

- [x] Domain: customers, KYC, accounts, money
- [x] Application: commands and handlers
- [x] Infrastructure: PostgreSQL, EF Core, migrations, integration tests
- [ ] API: endpoints, mapping errors to HTTP status codes, ProblemDetails, Swagger
- [ ] Queries and read models (account details, customer's accounts)
- [ ] Domain events with an outbox
- [ ] Ledger and transfers between accounts, with idempotency keys
- [ ] Authentication: customers can only see their own accounts
