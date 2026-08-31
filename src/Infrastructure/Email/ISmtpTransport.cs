namespace Infrastructure.Email;

public sealed record SmtpEnvelope(
    string Destination,
    string Subject,
    string Body);

public interface ISmtpTransport
{
    Task SendAsync(
        SmtpEnvelope envelope,
        CancellationToken cancellationToken);
}
