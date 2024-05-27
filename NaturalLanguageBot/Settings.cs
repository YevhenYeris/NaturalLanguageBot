using System.Security;

namespace NaturalLanguageBot;

public sealed class Settings
{
    public required string TelegramAccessKey { get; set; }

    public required string EmojisCsvPath { get; set; }
}