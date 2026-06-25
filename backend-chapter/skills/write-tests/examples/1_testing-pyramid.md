# Testing Pyramid — Decision Guide

> Example **1** — When to choose unit, component, or integration tests.

**Pyramid volume:** **many** unit → **some** component → **few** integration.

## Unit test when

- **Default** for testable production code — aim for close to **100%** line and branch coverage in unit tests
- Testing pure mapping logic with many edge cases
- Testing a single enrichment step with one mocked client
- Testing null-guard enforcement via `ArgumentsNullChecker`
- Testing HTTP status code mapping in a thin function wrapper (inject **real** Api/Infra mappers — do not mock `I*Mapper`)

**Do not** unit-test framework wiring, DI registration, or **Infra client implementations** — cover those with component and integration tests. **Do not** mock mappers — use `new FooWebhookMapper()`; mapping edge cases belong in `{Mapper}Tests.cs`.

## Component test when

- Verifying an HTTP receiver returns 202 and calls mocked blob, Service Bus, and downstream HTTP clients
- Verifying a processor publishes the correct outbound payload from a programmatic order builder
- Verifying file-driven JSON fixtures match expected outbound events
- Verifying Service Bus dead-letter behaviour with mocked `ServiceBusMessageActions`

**Default choice** for new Azure Function receiver/processor pairs.

## Integration test when

**By environment:**

| Environment | Run | Coverage |
|-------------|-----|----------|
| **TST** (and ACC if used) | `@smoke` + `@integration` | At least one `@integration` feature per functional flow / feature |
| **PRD** | `@smoke` only | Connectivity and auth enforcement — no real side-effect tests |

Examples:

- `@smoke` — deployed endpoints reject requests without function key
- `@integration` — real webhook produces a backup blob matching a fixture
- `@integration` — GET query endpoints against known test-environment data

**Never** run `@integration` tests in production.

## Example decision

| Change | Unit | Component | Integration |
|--------|------|-----------|-------------|
| New receiver function | Status codes + null checks (real mapper) | Full POST → mock blob, SB, and HTTP clients | TST: one `@integration` flow; PRD: `@smoke` only |
| New mapper with 10 edge cases | All edge cases | One happy-path scenario | No |
| New flow / feature | Flow handler unit tests | File-driven + programmatic | TST: one `@integration` per flow; PRD: `@smoke` only |
