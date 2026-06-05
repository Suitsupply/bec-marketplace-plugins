---
name: analyze-test-suite
description: >-
  Testing analysis skill. Assesses whether the test suite gives deployment
  confidence — the ability to merge and ship without manual verification.
  Produces test recommendations for a ticket's changes or a codebase
  area, plus a suite health verdict with a rework story recommendation when the
  setup undermines that confidence.
---

# Test Analysis

The goal is **deployment confidence**: the ability to merge and deploy without manually verifying that nothing broke.

Two readers:

- **The reporter** — can push back. Surface anything that needs their judgement.
- **The implementing agent** — cannot push back. Treats the plan as the spec; anything missing gets re-derived and may diverge.

Produce two ordered blocks:

1. **For the reporter** — suite health verdict:

   *Core question: if a change introduced a regression in this codebase today, would the test suite catch it — and would you trust the result?*

   Three axes, following a test's life — **enable** the code, **construct** the test, **operate** the suite. Flag where any falls short:

   - **Enable** *(code under test)* — can a test get at the behavior to check it, or does the code's shape block it — logic tangled with I/O, dependencies hard-wired instead of injected, one method doing too much to exercise in isolation? When this fails it is the root cause: the tests can't be good because the code won't let them.

   - **Construct** *(test code)* — once testable, is the test built to prove real behavior? Its three phases each fail independently:
     - *Arrange — realism:* does a green test mean it works against the real service, or only against mocked infrastructure?
     - *Act — behavioral coverage:* is the behavior exercised the way a real caller exercises it, or only in dissected units?
     - *Assert — observable outcomes:* does the test check what callers observe (response, persisted state, emitted events)?
     When internals are asserted *because the code exposes no observable outcome*, the fix is in the code under test.

   - **Operate** *(pipeline & runtime)* — once built, does the suite actually govern shipping? It must run at the right gate (unit and integration on every commit; e2e before the staging slot swap) and be trusted enough that a red stops the merge and a green clears it. Trust dies to three things: non-determinism, slowness, and dead tests. Evaluate by what the team does with the suite, not just whether the tests exist.

   **Verdict — use one of three:**
   - `Healthy` — suite gives deployment confidence; no structural action needed
   - `Needs attention: <one sentence>` — gaps in coverage but infrastructure is sound
   - `Recommend rework story: <one sentence root cause>` — describe what the story should address; do not create it

   Never fold suite rework into the current ticket's scope. The current ticket gets tests written within the existing setup, or the minimum new infrastructure its own tests require. Structural improvement is always a separate story.

2. **For the implementing agent** — concrete test plan. Specific enough the next agent writes the agreed tests, not a near-miss: skip the scenarios it will trivially discover, name the ones where a wrong guess diverges from the intended coverage.

   **Unit tests** — per entry: `Module.method` → the edge case and why the integration layer can't cover it economically.

   **Integration/Component tests** — the core of the plan. Per scenario:
   - Interface entry point (HTTP method + path, event type, CLI invocation, or component + interaction)
   - Input: payload, headers, seed state, or props
   - Expected: response or output, plus observable side effects (DB state, events published, DOM state)

   State testcontainer images and factory setup once upfront if not already present in the suite.

   **E2E tests** — per entry: the cross-service journey and why the integration layer would miss the regression. Default: none.

   When the existing suite is broken, still produce recommendations showing what tests should look like — the rework story needs a concrete target.

## Writing the analysis

- **Codebase-grounded.** Cite the test files behind every claim — name the suite that mocks the database, the missing factory. Anything you lean on but cannot verify is an `**Assumption:**` for the reporter to validate.
- **Opinionated.** Give the verdict; name the alternative only when the call was close.
- **Analysis only.** No writing tests, no branches, no commits.

## Doctrine

The default approach is the **testing diamond**: few unit tests, many integration/component tests, few e2e tests. The middle layer earns deployment confidence because it tests real behavior through the real interface against real dependencies — no mocked databases, brokers, or external services.

- **Integration/Component tests (the majority):** Test through the real interface against real dependencies. This is the default layer. If a scenario can be tested here, it must be.
- **Unit tests (few — edge cases only):** Pure functions with logic too complex or too varied to reach economically through the real interface. The argument is always "the integration layer cannot cover this economically" — never "it's faster to write."
- **E2E tests (few):** Complete user journeys that span services or represent business-critical flows. Justified only when a regression in the cross-service path would not be caught at the integration layer.

## Dotnet

### Unit Tests

NUnit + Moq — applies to all interface types.

### HTTP API

**Integration Tests** — web application factory + Testcontainers. State container images and factory setup once upfront if not already present in the suite.

**E2E Tests** — NUnit + `HttpClient` against the staging slot. Run with `dotnet test --logger trx` for pipeline-integrated results. Cross-service journeys only

### Event-driven consumer

**Integration Tests** — Testcontainers for the message broker (Azure Service Bus emulator). Publish a message to the real broker; await observable side effects (DB state, downstream events emitted) via polling with timeout.

### CLI tool

Keep entry point thin; inject dependencies (console, file system, services) so command handlers can be tested without the binary.

**Integration Tests** — invoke the entry point in-process (direct `Main` call) with Testcontainers for infrastructure dependencies; assert on exit code, stdout/stderr, and observable side effects.

**E2E Tests** — out-of-process via `ProcessStartInfo` against the compiled binary.

### Pure library

**Component Tests** — if the library performs no I/O, component tests are the primary layer (not the exception): call the public API directly with real internals, no mocks.

**Integration Tests** — if the library wraps infrastructure (database, cache, external API), Testcontainers; assert on observable side effects — same pattern as HTTP API, without the web application factory.

## React + TypeScript

### Unit Tests

Vitest + `vi.mock()` — pure utility functions and hooks with logic too complex to reach economically through component rendering.

### Component Tests

Vitest + React Testing Library against jsdom — the primary layer. Render the component, interact via `userEvent`, assert on the DOM. Wrap with `renderWithProviders()` for QueryClient and Router dependencies; use `waitFor()` for async assertions. Test files colocated in `__tests__` next to the component.

### E2E Tests

Playwright — full browser against the deployed application; cross-page journeys and critical user flows only. Few, default none.


