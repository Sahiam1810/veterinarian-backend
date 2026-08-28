namespace Application.Agent.Abstractions;

public interface IUserAccessTokenProvider
{
    string GetRequiredAccessToken();
}
