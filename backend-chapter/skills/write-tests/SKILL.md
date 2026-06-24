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
        │  Component  │  Many — in-process, Infra mocked
        ├─────────────┤
        │    Unit     │  Some — isolated classes, all deps mocked
        └─────────────┘
```

**Goal:** deployment confidence — merge and ship without manual verification.

## When to use each tier

| Tier | Project | What it proves | When to add |
|------|---------|----------------|-------------|
| **Unit** | `test/{ServiceName}.UnitTests/` | Single class logic with mocked dependencies | Edge cases unit tests cover cheaper than component tests |
| **Component** | `test/{ServiceName}.ComponentTests/` | HTTP/Service Bus function in-process end-to-end; Infra fully mocked | Default for new Azure Function flows |
| **Integration** | `test/{ServiceName}.IntegrationTests/` | Live deployed host; real blob/HTTP side effects | Smoke auth checks; critical paths post-deploy |

Apply **analyze-test-suite** when assessing suite health or planning coverage for a ticket.

## Routing

| Scenario | Skill |
|----------|-------|
| Test a single class in isolation | **write-unit-tests** |
| Test function in-process with mocked Infra | **write-component-tests** |
| Test against deployed environment | **write-integration-tests** |
| Suite health / test plan for Jira ticket | **analyze-test-suite** |

## Shared conventions

- **NUnit 3** for all test projects
- **Reqnroll 3.3.4** (`Reqnroll.NUnit`) for component and integration tests
- **Moq** + **AutoFixture** in unit and component tests; **no Moq** in integration tests
- **coverlet** for unit/component coverage (`CollectCoverage=true`)
- Standard `PropertyGroup` blocks on every test `.csproj` — see **dotnet-best-practices** [reference/csproj.md](../dotnet-best-practices/reference/csproj.md) (`IsPackable` false; coverlet on unit/component)
- Mirror `src/` folder structure under each test project

## Cross-skill rule

After writing production code (**write-src-code**), apply the matching test sub-skill before considering work done.

## Examples

See [examples/testing-pyramid.md](examples/testing-pyramid.md).

## Sub-skills

- **write-unit-tests** — NUnit, base/derived classes, `FixtureFactory`, `ArgumentsNullChecker`
- **write-component-tests** — Reqnroll, `ApplicationFactory`, feature file naming
- **write-integration-tests** — `@smoke` / `@integration`, runsettings, side-effect polling
