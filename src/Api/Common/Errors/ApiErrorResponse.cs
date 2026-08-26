namespace Api.Common.Errors;
public sealed record ApiErrorResponse(
    DateTimeOffset Timestamp,
    int Status,
    string Error,
    string Message,
    string Path,
    IReadOnlyList<FieldViolationResponse> Violations);