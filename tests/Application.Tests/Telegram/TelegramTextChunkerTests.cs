using Application.Telegram.Messages;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class TelegramTextChunkerTests
{
    [Fact]
    public void Short_text_remains_in_one_chunk()
    {
        Assert.Equal(["respuesta"], TelegramTextChunker.Split("respuesta"));
    }

    [Fact]
    public void Long_text_is_split_without_losing_characters()
    {
        var text = new string('a', 3000) + "\n\n" + new string('b', 3000);

        var chunks = TelegramTextChunker.Split(text);

        Assert.Equal(2, chunks.Count);
        Assert.All(chunks, chunk => Assert.InRange(chunk.Length, 1, 4096));
        Assert.Equal(text, string.Concat(chunks));
    }
}
