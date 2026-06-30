Feature: Foo Created Processor (File-Driven)
  End-to-end component test using JSON fixtures under Scenarios/FooCreated/.

  Background:
    Given the outbound publisher returns message id "test-message-id"

  Scenario Outline: <scenarioFolder> - processor output matches fixture
    Given the foo scenario is loaded from folder "<scenarioFolder>"
    When the foo created processor processes the file-driven scenario
    Then the published outbound event matches the expected fixture

    Examples:
      | scenarioFolder   |
      | HappyPath-Example |
