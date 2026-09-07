using Application.Clients.Errors;
using Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Infrastructure.Persistence;

// Traduce carreras ORA-00001 del índice único de teléfono a ConflictException tipada.
public static class OracleClientPhoneConflictMapper
{
    public const string UniqueIndexName = "UX_CLIENTS_PHONE_NUMBER";

    public static bool TryMapToConflict(
        DbUpdateException exception,
        out ConflictException? conflict)
    {
        if (IsClientPhoneNumberUniqueViolation(exception))
        {
            conflict = new ConflictException(
                "Ya existe un cliente con ese número de teléfono.",
                ClientErrorCodes.PhoneNumberAlreadyExists);
            return true;
        }

        conflict = null;
        return false;
    }

    public static bool IsClientPhoneNumberUniqueViolation(Exception exception)
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
