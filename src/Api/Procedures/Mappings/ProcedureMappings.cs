using Api.Procedures.Dtos;
using Application.Procedures.UseCases;
using Domain.Procedures.Entities;

namespace Api.Procedures.Mappings;

public static class ProcedureMappings
{
    public static ProcedureDto ToResponse(this Procedure procedure)
    {
        return new ProcedureDto(
            procedure.Id,
            procedure.Name,
            procedure.Code,
            procedure.IsActive,
            procedure.Price,
            procedure.CreatedAt,
            procedure.UpdatedAt);
    }

    public static IEnumerable<ProcedureDto> ToResponse(this IEnumerable<Procedure> procedures)
    {
        return procedures.Select(p => p.ToResponse());
    }

    public static CreateProcedureCommand ToCommand(this CreateProcedureDto dto)
    {
        return new CreateProcedureCommand(dto.Name, dto.Code, dto.IsActive, dto.Price);
    }

    public static UpdateProcedureCommand ToCommand(this UpdateProcedureDto dto, Guid id)
    {
        return new UpdateProcedureCommand(id, dto.Name, dto.Code, dto.IsActive, dto.Price);
    }
}
