namespace Api.Common.Errors;

public sealed record FieldViolationResponse(
    string Field,
    string Message);