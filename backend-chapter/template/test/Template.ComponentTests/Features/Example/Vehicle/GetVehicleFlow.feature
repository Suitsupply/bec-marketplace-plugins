Feature: Get Vehicle Flow
  In order to retrieve a Star Wars vehicle
  As an API consumer
  I want the endpoint to return the mapped vehicle when found,
  404 when not found, and 500 on unexpected failures

  Scenario: Vehicle is found and returned as mapped response
    Given the SWAPI client returns a vehicle for id 4
    When I send a GET request to "/api/vehicles/4"
    Then the response status code should be 200
    And the response body contains the vehicle name "Sand Crawler"

  Scenario: Vehicle is not found returns 404
    Given the SWAPI client returns no vehicle for id 99999
    When I send a GET request to "/api/vehicles/99999"
    Then the response status code should be 404

  Scenario: A SWAPI client failure returns 500
    Given the SWAPI client throws for vehicle id 4
    When I send a GET request to "/api/vehicles/4"
    Then the response status code should be 500
