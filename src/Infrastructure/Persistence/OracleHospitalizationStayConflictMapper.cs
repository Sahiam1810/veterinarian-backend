using Application.Common.Exceptions;
using Application.HospitalizationStays.Errors;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Infrastructure.Persistence;

// Traduce carreras ORA-00001 del índice único de estancia activa por mascota a ConflictException.
// El índice es funcional (CASE WHEN ESTADO = 0 THEN CLIENT_PET_ID END) y se crea con SQL en la
// migración: EF no lo modela, por eso no aparece en HospitalizationStayConfiguration.
public static class OracleHospitalizationStayConflictMapper
{
    public const string UniqueIndexName = "UX_HOSP_STAY_ACTIVE_PER_PET";

    public static bool TryMapToConflict(
        DbUpdateException exception,
        out ConflictException? conflict)
    {
        if (IsActiveStayPerPetUniqueViolation(exception))
        {
            conflict = new ConflictException(
                HospitalizationStayErrorCodes.ActiveStayAlreadyExistsMessage,
                HospitalizationStayErrorCodes.ActiveStayAlreadyExists);
            return true;
        }

        conflict = null;
        return false;
    }

    public static bool IsActiveStayPerPetUniqueViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var message = current.Message ?? string.Empty;
            var isOra00001 = current is OracleException { Number: 1 }
                || message.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase);

            if (isOra00001
                && message.Contains(UniqueIndexName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
