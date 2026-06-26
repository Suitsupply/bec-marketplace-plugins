Feature: Get Person Flow
  In order to retrieve a Star Wars person
  As an API consumer
  I want the endpoint to return the mapped person when found,
  404 when not found, and 500 on unexpected failures

  Scenario: Person is found and returned as mapped response
    Given the SWAPI client returns a person for id 1
    When I send a GET request to "/api/person/1"
    Then the response status code should be 200
    And the response body contains the person name "Luke Skywalker"

  Scenario: Person is not found returns 404
    Given the SWAPI client returns no person for id 99999
    When I send a GET request to "/api/person/99999"
    Then the response status code should be 404

  Scenario: A SWAPI client failure returns 500
    Given the SWAPI client throws for id 1
    When I send a GET request to "/api/person/1"
    Then the response status code should be 500
