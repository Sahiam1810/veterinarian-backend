namespace Application.Telegram.Messages;

public static class TelegramTextChunker
{
    public const int MaximumLength = 4096;

    public static IReadOnlyList<string> Split(
        string text,
        int maximumLength = MaximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (maximumLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        }

        var chunks = new List<string>((text.Length + maximumLength - 1) / maximumLength);
        for (var offset = 0; offset < text.Length; offset += maximumLength)
        {
            chunks.Add(text.Substring(offset, Math.Min(maximumLength, text.Length - offset)));
        }

        return chunks;
    }
}
