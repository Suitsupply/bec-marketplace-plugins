# Testing Pyramid — Decision Guide

## Unit test when

- Testing pure mapping logic with many edge cases
- Testing a single enrichment step with one mocked client
- Testing null-guard enforcement via `ArgumentsNullChecker`
- Testing HTTP status code mapping in a thin function wrapper

**Do not** unit-test framework wiring or DI registration.

## Component test when

- Verifying an HTTP receiver returns 202 and calls blob + Service Bus mocks
- Verifying a processor publishes the correct outbound payload from a programmatic order builder
- Verifying file-driven JSON fixtures match expected outbound events
- Verifying Service Bus dead-letter behaviour with mocked `ServiceBusMessageActions`

**Default choice** for new Azure Function receiver/processor pairs.

## Integration test when

- Verifying deployed endpoints reject requests without function key (`@smoke`)
- Verifying a real webhook produces a backup blob matching a fixture (`@integration`)
- Verifying GET query endpoints against known test-environment data

**Never** run `@integration` tests against production-like environments.

## Example decision

| Change | Unit | Component | Integration |
|--------|------|-----------|-------------|
| New receiver function | Status codes + null checks | Full POST → mock blob/SB | Optional smoke 401 |
| New mapper with 10 edge cases | All edge cases | One happy-path scenario | No |
| New processor flow | Flow handler unit tests | File-driven + programmatic | One file-driven against TST |
