using Application.Common.Abstractions;
using Domain.Users.ValueObjects;
using FluentValidation;

namespace Application.Users.UseCase;

public sealed class CreateUserCommandValidator
    : AbstractValidator<CreateUserCommand>
{
    private const string ClientRoleName = "Cliente";

    public CreateUserCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(command => command.FullName)
            .NotEmpty()
            .WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(150)
            .WithMessage("El nombre completo no puede superar los 150 caracteres.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(UserEmail.MaxLength)
            .WithMessage(
                $"El correo electrónico no puede superar los {UserEmail.MaxLength} caracteres.")
            .EmailAddress()
            .WithMessage("El correo electrónico no tiene un formato válido.");

        // Cliente nunca se loguea (solo interactúa vía chatbot): no lleva
        // contraseña. Cualquier otro rol la sigue exigiendo como antes.
        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .WhenAsync(async (command, ct) => !await IsClientRoleAsync(unitOfWork, command.RoleId, ct));

        RuleFor(command => command.Password)
            .Empty()
            .WithMessage("Los usuarios con rol Cliente no deben tener contraseña; ese rol solo interactúa vía chatbot.")
            .WhenAsync(async (command, ct) => await IsClientRoleAsync(unitOfWork, command.RoleId, ct));

        RuleFor(command => command.RoleId)
            .NotEmpty()
            .WithMessage("Debe asignar un rol al usuario.");
    }

    private static async Task<bool> IsClientRoleAsync(
        IUnitOfWork unitOfWork, Guid roleId, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RolesRepository.GetByIdAsync(roleId, cancellationToken);
        return role is not null && string.Equals(role.Name.Value, ClientRoleName, StringComparison.Ordinal);
    }
}
