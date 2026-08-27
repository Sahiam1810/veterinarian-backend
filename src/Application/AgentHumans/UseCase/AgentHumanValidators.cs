using FluentValidation;

namespace Application.AgentHumans.UseCase;

public sealed class CreateAgentHumanCommandValidator : AbstractValidator<CreateAgentHumanCommand>
{
    public CreateAgentHumanCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");
    }
}

public sealed class GetAgentHumanByIdQueryValidator : AbstractValidator<GetAgentHumanByIdQuery>
{
    public GetAgentHumanByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del agente humano es obligatorio.");
    }
}

public sealed class GetAgentHumansByUserIdQueryValidator : AbstractValidator<GetAgentHumansByUserIdQuery>
{
    public GetAgentHumansByUserIdQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty()
            .WithMessage("El identificador del usuario es obligatorio.");
    }
}

public sealed class UpdateAgentHumanCommandValidator : AbstractValidator<UpdateAgentHumanCommand>
{
    public UpdateAgentHumanCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del agente humano es obligatorio.");
    }
}

public sealed class ActivateAgentHumanCommandValidator : AbstractValidator<ActivateAgentHumanCommand>
{
    public ActivateAgentHumanCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del agente humano es obligatorio.");
    }
}

public sealed class DeactivateAgentHumanCommandValidator : AbstractValidator<DeactivateAgentHumanCommand>
{
    public DeactivateAgentHumanCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del agente humano es obligatorio.");
    }
}
