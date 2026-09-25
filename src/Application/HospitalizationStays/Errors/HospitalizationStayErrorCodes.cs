namespace Application.HospitalizationStays.Errors;

// Códigos estables de Hospitalización para problem+json (front).
public static class HospitalizationStayErrorCodes
{
    // La mascota ya tiene una estancia activa (chequeo del handler o índice único en Oracle).
    public const string ActiveStayAlreadyExists = "HospitalizationStays.ActiveStayAlreadyExists";
    public const string ActiveStayAlreadyExistsMessage = "La mascota ya tiene una estancia activa.";
}
