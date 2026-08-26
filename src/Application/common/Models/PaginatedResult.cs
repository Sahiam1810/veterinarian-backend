
namespace Application.Common.Models;
public sealed record PaginatedResult<T>(
    IReadOnlyCollection<T> Items,
    PaginationMetadata Pagination);