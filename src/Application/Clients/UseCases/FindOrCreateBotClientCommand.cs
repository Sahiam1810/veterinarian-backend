using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Security;
using Application.Telegram.Abstractions;
using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using Domain.Users.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Clients.UseCases;

public sealed record FindOrCreateBotClientCommand(
    string IdentificationNumber,
    string FullName,
    string Email,
    string? PhoneNumber = null,
    long? TelegramUserId = null,
    long? TelegramChatId = null) : IRequest<FindOrCreateBotClientResult>;

public sealed record FindOrCreateBotClientResult(
    Guid ClientId,
    Guid UserId,
    Guid UserAccountId,
    string IdentificationNumber,
    bool Created,
    string AccessToken);

public sealed class FindOrCreateBotClientCommandHandler(
    IUnitOfWork unitOfWork,
    IAgentDelegatedIdentityProvider identityProvider,
    ILogger<FindOrCreateBotClientCommandHandler> logger)
    : IRequestHandler<FindOrCreateBotClientCommand, FindOrCreateBotClientResult>
{
    private const string ActiveStatus = "Activo";

    public async Task<FindOrCreateBotClientResult> Handle(
        FindOrCreateBotClientCommand request,
        CancellationToken cancellationToken)
    {
        var identification = ClientIdentificationNumber.Create(request.IdentificationNumber).Value;
        var fullName = request.FullName.Trim();
        var email = UserEmail.Create(request.Email).Value;
        var phone = ClientPhoneNumber.CreateOptional(request.PhoneNumber);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new BadRequestException("El nombre es obligatorio.");
        }

        // TelegramUserId/ChatId solo correlacionan el request; no se crea soft-link permanente.
        _ = request.TelegramUserId;
        _ = request.TelegramChatId;

        var existing = await unitOfWork.ClientsRepository.GetByIdentificationNumberAsync(
            identification,
            cancellationToken);
        if (existing is not null)
        {
            return await CompleteExistingAsync(existing, fullName, email, phone, cancellationToken);
        }

        try
        {
            return await CreateNewAsync(identification, fullName, email, phone, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var raced = await unitOfWork.ClientsRepository.GetByIdentificationNumberAsync(
                identification,
                cancellationToken);
            if (raced is null)
            {
                throw;
            }

            // Carrera de unicidad de cédula: tratar como "ya existe".
            logger.LogInformation(
                "Find-or-create hit identification race; reusing ClientId={ClientId}",
                raced.Id);
            return await CompleteExistingAsync(raced, fullName, email, phone, cancellationToken);
        }
    }

    private async Task<FindOrCreateBotClientResult> CreateNewAsync(
        string identification,
        string fullName,
        string email,
        ClientPhoneNumber? phone,
        CancellationToken cancellationToken)
    {
        var clientRole = await unitOfWork.RolesRepository.GetByNameAsync(
            WebPlatformAccess.ClientRoleName,
            cancellationToken)
            ?? throw new NotFoundException("No está configurado el rol Cliente.");

        if (await unitOfWork.UsersRepository.ExistsByEmailAsync(email, cancellationToken)
            || await unitOfWork.UserAccountsRepository.ExistsByMailAsync(email, cancellationToken))
        {
            throw new ConflictException("Ya existe un usuario con ese correo electrónico.");
        }

        if (phone is not null
            && await unitOfWork.ClientsRepository.ExistsByPhoneAsync(phone.Value, cancellationToken))
        {
            throw new ConflictException("Ya existe un cliente con ese número de teléfono.");
        }

        FindOrCreateBotClientResult? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var again = await unitOfWork.ClientsRepository.GetByIdentificationNumberAsync(
                identification,
                token);
            if (again is not null)
            {
                result = await CompleteExistingAsync(again, fullName, email, phone, token);
                return;
            }

            string username;
            do
            {
                username = $"bot_{Guid.NewGuid():N}"[..30];
            }
            while (await unitOfWork.UserAccountsRepository.ExistsByUsernameAsync(username, token));

            var user = new UserEntity(fullName, email, passwordHash: null, clientRole.Id);
            var account = new UserAccountEntity(user.Id, username, email, ActiveStatus);
            var client = new ClientEntity(
                user.Id,
                identification,
                address: null,
                phoneNumber: phone?.Value);

            await unitOfWork.UsersRepository.AddAsync(user, token);
            await unitOfWork.UserAccountsRepository.AddAsync(account, token);
            await unitOfWork.ClientsRepository.AddAsync(client, token);
            await unitOfWork.SaveChangesAsync(token);

            var identity = await identityProvider.GetAsync(user.Id, token);
            result = new FindOrCreateBotClientResult(
                client.Id,
                user.Id,
                account.Id,
                identification,
                Created: true,
                identity.AccessToken);
        }, cancellationToken);

        logger.LogInformation(
            "Bot client created. ClientId={ClientId} UserId={UserId}",
            result!.ClientId,
            result.UserId);
        return result;
    }

    private async Task<FindOrCreateBotClientResult> CompleteExistingAsync(
        ClientEntity client,
        string fullName,
        string email,
        ClientPhoneNumber? phone,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UsersRepository.GetByIdAsync(client.UserId, cancellationToken)
            ?? throw new NotFoundException("Usuario del cliente no encontrado.");
        if (!user.IsActive)
        {
            throw new ConflictException("El cliente asociado a esa cédula no está activo.");
        }

        await SoftFillContactAsync(client, user, fullName, email, phone, cancellationToken);

        var account = await EnsureUserAccountAsync(user, email, cancellationToken);
        var identity = await identityProvider.GetAsync(user.Id, cancellationToken);
        return new FindOrCreateBotClientResult(
            client.Id,
            user.Id,
            account.Id,
            client.IdentificationNumber.Value,
            Created: false,
            identity.AccessToken);
    }

    private async Task SoftFillContactAsync(
        ClientEntity client,
        UserEntity user,
        string fullName,
        string email,
        ClientPhoneNumber? phone,
        CancellationToken cancellationToken)
    {
        var userChanged = false;
        var nextName = string.IsNullOrWhiteSpace(user.FullName) ? fullName : user.FullName;
        var nextEmail = string.IsNullOrWhiteSpace(user.Email.Value) ? email : user.Email.Value;
        if (!string.Equals(user.FullName, nextName, StringComparison.Ordinal)
            || !string.Equals(user.Email.Value, nextEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(user.Email.Value, nextEmail, StringComparison.OrdinalIgnoreCase)
                && await unitOfWork.UsersRepository.ExistsByEmailAsync(
                    nextEmail,
                    cancellationToken,
                    excludedId: user.Id))
            {
                throw new ConflictException("Ya existe un usuario con ese correo electrónico.");
            }

            user.Update(nextName, nextEmail, user.RoleId);
            await unitOfWork.UsersRepository.UpdateAsync(user, cancellationToken);
            userChanged = true;
        }

        var clientChanged = false;
        var nextPhone = client.PhoneNumber ?? phone;
        if (client.PhoneNumber is null && phone is not null)
        {
            if (await unitOfWork.ClientsRepository.ExistsByPhoneAsync(phone.Value, cancellationToken))
            {
                throw new ConflictException("Ya existe un cliente con ese número de teléfono.");
            }

            client.Update(
                client.UserId,
                client.IdentificationNumber.Value,
                client.Address.Value,
                client.RegistrationDate,
                phone.Value);
            await unitOfWork.ClientsRepository.UpdateAsync(client, cancellationToken);
            clientChanged = true;
        }

        if (userChanged || clientChanged)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _ = nextPhone;
    }

    private async Task<UserAccountEntity> EnsureUserAccountAsync(
        UserEntity user,
        string preferredEmail,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountsRepository.GetByUserIdAsync(
            user.Id,
            cancellationToken);
        if (account is not null)
        {
            if (!string.Equals(account.Status, ActiveStatus, StringComparison.Ordinal))
            {
                throw new ConflictException("La cuenta del cliente no está activa.");
            }

            return account;
        }

        var email = string.IsNullOrWhiteSpace(user.Email.Value) ? preferredEmail : user.Email.Value;
        if (await unitOfWork.UserAccountsRepository.ExistsByMailAsync(email, cancellationToken))
        {
            throw new ConflictException("Ya existe una cuenta con ese correo electrónico.");
        }

        string username;
        do
        {
            username = $"bot_{Guid.NewGuid():N}"[..30];
        }
        while (await unitOfWork.UserAccountsRepository.ExistsByUsernameAsync(
                   username,
                   cancellationToken));

        account = new UserAccountEntity(user.Id, username, email, ActiveStatus);
        await unitOfWork.UserAccountsRepository.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return account;
    }
}
