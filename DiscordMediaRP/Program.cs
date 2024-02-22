// See https://aka.ms/new-console-template for more information

using DiscordMediaRP;
using Microsoft.Extensions.Logging;

int loglevelIndex = Array.IndexOf(args, "-l");
LogLevel? level = null;
if(loglevelIndex > -1)
    if(loglevelIndex + 1 < args.Length)
        level = loglevelIndex < args.Length ? Enum.Parse<LogLevel>(args[loglevelIndex + 1]) : null;
    else
        throw new IndexOutOfRangeException(nameof(loglevelIndex));

int discordKeyIndex = Array.IndexOf(args, "-d");
string discordKey;
if (loglevelIndex > -1)
    if(discordKeyIndex + 1 < args.Length)
        discordKey = args[discordKeyIndex + 1];
    else
        throw new IndexOutOfRangeException("No Discord ApplicationKey provided");
else
    throw new ArgumentNullException(nameof(discordKey));

int imageKeyIndex = Array.IndexOf(args, "-i");
string? imageKey = null;
if(imageKeyIndex > -1)
    if (imageKeyIndex + 1 < args.Length)
        imageKey = args[imageKeyIndex + 1];
    else
        throw new IndexOutOfRangeException(nameof(imageKeyIndex));

DisMediaRP _ = new (discordKey, level, imageKey);