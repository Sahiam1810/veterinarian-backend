namespace Application.Verification.Abstractions;

// Código OTP en claro (solo para envío) y su hash persistible.
public sealed record GeneratedOtp(string Code, string Hash);

// Protector genérico de OTP (hash con pepper, verificación en tiempo constante).
public interface IOtpProtector
{
    GeneratedOtp Create();

    bool Verify(string code, string expectedHash);

    string HashEmail(string normalizedEmail);

    string HashPhone(string normalizedPhone);
}
