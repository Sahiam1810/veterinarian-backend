using Application.Common.Results;
using Application.Telegram.Abstractions;
using MediatR;

namespace Application.Telegram.Registration;

public sealed record PendingTelegramRegistration(DateTime ExpiresAt);

public sealed record GetTelegramRegistrationSessionQuery(string Token)
    : IRequest<Result<PendingTelegramRegistration>>;

public sealed class GetTelegramRegistrationSessionQueryHandler(
    ITelegramUnitOfWork unitOfWork,
    ITelegramRegistrationProtector protector,
    TimeProvider timeProvider)
    : IRequestHandler<GetTelegramRegistrationSessionQuery, Result<PendingTelegramRegistration>>
{
    public async Task<Result<PendingTelegramRegistration>> Handle(
        GetTelegramRegistrationSessionQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result<PendingTelegramRegistration>.Failure(
                TelegramRegistrationErrors.InvalidOrExpired);
        }

        var session = await unitOfWork.RegistrationSessionsRepository
            .GetByCompletionTokenHashAsync(
                protector.HashCompletionToken(request.Token), cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (session?.CompletionExpiresAt is null || now >= session.CompletionExpiresAt)
        {
            return Result<PendingTelegramRegistration>.Failure(
                TelegramRegistrationErrors.InvalidOrExpired);
        }

        return Result<PendingTelegramRegistration>.Success(
            new PendingTelegramRegistration(session.CompletionExpiresAt.Value));
    }
}
