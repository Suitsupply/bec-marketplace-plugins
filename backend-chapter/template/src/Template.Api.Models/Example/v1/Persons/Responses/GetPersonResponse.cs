using Template.Api.Models.Example.v1.Persons.Models;

namespace Template.Api.Models.Example.v1.Persons.Responses;

public sealed record GetPersonResponse(
    int Id,
    string Name,
    string Height,
    string Mass,
    Address Address);