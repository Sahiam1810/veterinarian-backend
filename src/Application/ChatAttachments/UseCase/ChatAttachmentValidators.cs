using FluentValidation;

namespace Application.ChatAttachments.UseCase;

public sealed class CreateChatAttachmentCommandValidator
    : AbstractValidator<CreateChatAttachmentCommand>
{
    public CreateChatAttachmentCommandValidator()
    {
        RuleFor(command => command.ChatMessageId)
            .NotEmpty()
            .WithMessage("El identificador del mensaje es obligatorio.");

        RuleFor(command => command.FileUrl)
            .NotEmpty()
            .WithMessage("La URL del archivo es obligatoria.");

        RuleFor(command => command.FileType)
            .NotEmpty()
            .WithMessage("El tipo de archivo es obligatorio.");

        RuleFor(command => command.FileName)
            .NotEmpty()
            .WithMessage("El nombre del archivo es obligatorio.");
    }
}

public sealed class GetChatAttachmentByIdQueryValidator
    : AbstractValidator<GetChatAttachmentByIdQuery>
{
    public GetChatAttachmentByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del adjunto es obligatorio.");
    }
}

public sealed class GetChatAttachmentsByMessageIdQueryValidator
    : AbstractValidator<GetChatAttachmentsByMessageIdQuery>
{
    public GetChatAttachmentsByMessageIdQueryValidator()
    {
        RuleFor(query => query.ChatMessageId)
            .NotEmpty()
            .WithMessage("El identificador del mensaje es obligatorio.");
    }
}
