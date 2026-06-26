---
name: write-tests
description: >-
  Backend Chapter testing pyramid and routing: when to write unit, component,
  or integration tests. Hub skill — links to write-unit-tests, write-component-tests,
  and write-integration-tests. Use when asked to write tests without specifying
  the tier, or when planning test coverage for a change.
---

# Write Tests

Hub skill for Backend Chapter test strategy. Works on Cursor and Claude Code.

## Testing pyramid

```
        ┌─────────────┐
        │ Integration │  Few — live deployed host, real infra
        ├─────────────┤
        │  Component  │  Some — in-process, Infra mocked
        ├─────────────┤
        │    Unit     │  Many — isolated classes; mock services/clients; static mappers
        └─────────────┘
```

**Goal:** deployment confidence — merge and ship without manual verification.

**Volume:** **many** unit tests, **some** component tests, **few** integration tests.

## When to use each tier

| Tier | Project | What it proves | When to add |
|------|---------|----------------|-------------|
| **Unit** | `test/{ServiceName}.UnitTests/` | Single class logic with mocked services and client interfaces; **static mappers called directly**; exercise branching and edge cases | Default for all testable App logic — aim for close to **100%** line and branch coverage (excludes wiring, DI, Infra clients) |
| **Component** | `test/{ServiceName}.ComponentTests/` | HTTP/Service Bus function in-process end-to-end; Infra fully mocked (blob, queue, **HTTP clients**, publishers) | Default for new Azure Function flows |
| **Integration** | `test/{ServiceName}.IntegrationTests/` | Live deployed host; real blob/HTTP side effects | **TST:** `@smoke` + at least one `@integration` per functional flow/feature; **PRD:** `@smoke` only |

Apply **analyze-test-suite** when assessing suite health or planning coverage for a ticket.

## Routing

| Scenario | Skill |
|----------|-------|
| Test a single class in isolation | **write-unit-tests** |
| Test function in-process with mocked Infra | **write-component-tests** |
| Test against deployed environment | **write-integration-tests** |
| Suite health / test plan for Jira ticket | **analyze-test-suite** |

## Shared conventions

- **NUnit 4** for all test projects (latest stable)
- **Reqnroll 3.3.4** (`Reqnroll.NUnit`) for component and integration tests
- **Moq** + **AutoFixture** in unit and component tests; **`AutoFixture.NUnit4` `[AutoData]` is unit-tier only** (component tests reference `AutoFixture` but do not use `[AutoData]`); **no Moq** in integration tests
- **coverlet** for **unit** coverage only (`CollectCoverage=true`) — component and integration tests do not collect coverage
- Standard `PropertyGroup` blocks on every test `.csproj` — see **dotnet-best-practices** [reference/5_csproj.md](../dotnet-best-practices/reference/5_csproj.md) (`IsPackable` false; coverlet on unit tests only)

### Project layout (by tier)

| Tier | Layout | Detail |
|------|--------|--------|
| **Unit** | **Mirrors `src/` exactly** | `test/{ServiceName}.UnitTests/{Layer}/…/{Class}Tests.cs` — same folder tree and namespaces as production, with `.UnitTests` in the namespace. See **write-unit-tests**. |
| **Component** | **Feature/flow layout** — not a `src/` mirror | `Features/`, `StepDefinitions/`, `Support/`, `Scenarios/` grouped by functional flow. See **write-component-tests**. |
| **Integration** | **Feature/flow layout** — not a `src/` mirror | Same Reqnroll shape as component (`Features/`, `StepDefinitions/`, `Support/`, `Scenarios/`) plus per-environment `*.runsettings`. See **write-integration-tests**. |

## Cross-skill rule

After writing production code (**dotnet-best-practices** references 12–18), apply the matching test sub-skill before considering work done.

## Examples

| # | File | Topic |
|---|------|-------|
| 1 | [1_testing-pyramid.md](examples/1_testing-pyramid.md) | When to use unit, component, or integration tests |

## Sub-skills

- **write-unit-tests** — NUnit, base/derived classes, `FixtureFactory`, `ArgumentsNullChecker`, static mappers
- **write-component-tests** — Reqnroll, `ApplicationFactory`, feature file naming
- **write-integration-tests** — `@smoke` / `@integration`, runsettings, side-effect polling
