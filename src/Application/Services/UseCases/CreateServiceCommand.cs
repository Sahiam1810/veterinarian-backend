using MediatR;

namespace Application.Services.UseCases;

public sealed record CreateServiceCommand(
    Guid TypeServiceId,
    string Name,
    int DurationMinutes,
    decimal Price,
    bool IsActive = true) : IRequest<Guid>;
