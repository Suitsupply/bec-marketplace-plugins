@smoke
Feature: Endpoint Authentication
  Verify receiver endpoints require a valid function key.

  Scenario Outline: Endpoints enforce access level
    When I send a <method> request to "<route>"
    Then the response status code should be <code>

    Examples:
      | method | route               | code |
      | GET    | /api/home           | 200  |
      | POST   | /api/foo/created    | 401  |
