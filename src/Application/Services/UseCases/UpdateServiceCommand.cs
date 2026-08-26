using MediatR;

namespace Application.Services.UseCases;

public sealed record UpdateServiceCommand(
    Guid Id,
    Guid TypeServiceId,
    string Name,
    int DurationMinutes,
    decimal Price,
    bool IsActive) : IRequest<bool>;
