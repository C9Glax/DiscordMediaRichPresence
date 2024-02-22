using Microsoft.Extensions.Logging;

namespace DiscordMediaRP;

public struct Config
{
    public required string DiscordKey;
    public LogLevel? LogLevel;
    public string? LargeImageKey;
    public bool? UseSpotify;
    public string[]? WebbrowserIgnoreSites;

    public Config WithDiscordKey(string key)
    {
        return this with { DiscordKey = key };
    }

    public Config WithLargeImageKey(string key)
    {
        return this with { LargeImageKey = key };
    }

    public Config WithLogLevel(LogLevel level)
    {
        return this with { LogLevel = level };
    }
}