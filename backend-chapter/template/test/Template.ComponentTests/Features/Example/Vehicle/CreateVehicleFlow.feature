Feature: Create Vehicle Flow
  In order to register a new Star Wars vehicle
  As an API consumer
  I want the endpoint to accept a valid request and return 202,
  and return 500 on unexpected failures

  Scenario: Valid request is accepted
    Given the SWAPI client accepts create vehicle requests
    And the request body is '{"name":"X-wing","model":"T-65 X-wing","manufacturer":"Incom Corporation","owner":{"name":"Luke Skywalker"}}'
    When I send a POST request to "/api/vehicles"
    Then the response status code should be 202

  Scenario: A SWAPI client failure returns 500
    Given the SWAPI client throws when creating a vehicle
    And the request body is '{"name":"X-wing","model":"T-65 X-wing","manufacturer":"Incom Corporation","owner":{"name":"Luke Skywalker"}}'
    When I send a POST request to "/api/vehicles"
    Then the response status code should be 500
