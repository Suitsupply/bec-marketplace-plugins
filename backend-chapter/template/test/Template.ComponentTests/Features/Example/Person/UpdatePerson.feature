Feature: Update Person Processor
  In order to process update-person messages
  As the Template host
  I want the debug route to accept a request and return 202 when processing succeeds

  Scenario: Debug route accepts a valid person request
    Given the SWAPI client returns a person for id 1
    And the request body is '{"id":1}'
    When I send a POST request to "/api/person/update/debug"
    Then the response status code should be 202

  Scenario: Debug route returns 500 when SWAPI fails
    Given the SWAPI client throws for id 1
    And the request body is '{"id":1}'
    When I send a POST request to "/api/person/update/debug"
    Then the response status code should be 500
