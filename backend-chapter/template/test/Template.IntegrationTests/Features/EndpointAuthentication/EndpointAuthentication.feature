@smoke
Feature: Endpoint Authentication
  In order to protect the example endpoints
  As the Azure Functions host
  I want the anonymous ServiceInfo home endpoint to be reachable
  while every function-key protected endpoint rejects unauthenticated requests

  Scenario: Home endpoint is anonymous and returns 200
    When I send a GET request to "/api/home"
    Then the response status code should be 200

  Scenario Outline: Protected endpoints reject requests without a function key
    When I send a <method> request to "<route>"
    Then the response status code should be 401

    Examples:
      | method | route                    |
      | GET    | /api/person/1            |
      | GET    | /api/vehicles/4          |
      | POST   | /api/vehicles            |
      | POST   | /api/person/update/debug |
