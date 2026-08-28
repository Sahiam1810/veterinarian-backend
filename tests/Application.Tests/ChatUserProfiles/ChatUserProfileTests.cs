using Application.ChatUserProfiles.UseCase;
using Domain.ChatUserProfiles.Entities;
using Xunit;

namespace Application.Tests.ChatUserProfiles;

public sealed class ChatUserProfileTests
{
    [Fact]
    public void Create_with_valid_user_id_sets_user_id()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var profile = ChatUserProfile.Create(userId, "Perfil", null, null);

        Assert.Equal(userId, profile.UserId);
        Assert.NotEqual(Guid.Empty, profile.Id);
    }

    [Fact]
    public void Create_with_empty_user_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ChatUserProfile.Create(Guid.Empty, null, null, null));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Get_by_user_id_query_with_empty_user_id_fails_validation()
    {
        var validator = new GetChatUserProfilesByUserIdQueryValidator();

        var result = validator.Validate(new GetChatUserProfilesByUserIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_command_with_empty_user_id_fails_validation()
    {
        var validator = new CreateChatUserProfileCommandValidator();

        var result = validator.Validate(new CreateChatUserProfileCommand(Guid.Empty, null, null, null));

        Assert.False(result.IsValid);
    }
}
