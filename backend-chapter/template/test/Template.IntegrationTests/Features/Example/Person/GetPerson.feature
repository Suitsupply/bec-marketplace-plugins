@integration
Feature: Get Person
  In order to retrieve Star Wars character data
  As an API consumer
  I want to fetch a person by id from the deployed Functions host

  Scenario: Get person returns Luke Skywalker
    When I send a GET request to the person endpoint for id 1
    Then the response status code should be 200
    And the person name should be "Luke Skywalker"
