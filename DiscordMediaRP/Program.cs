// See https://aka.ms/new-console-template for more information

using DiscordMediaRP;
using Microsoft.Extensions.Logging;

int loglevelIndex = Array.IndexOf(args, "-l") + 1;
LogLevel? level = loglevelIndex >= 0 && loglevelIndex < args.Length ? Enum.Parse<LogLevel>(args[loglevelIndex]) : null;
int discordKeyIndex = Array.IndexOf(args, "-d") + 1;
if (discordKeyIndex < 1 || discordKeyIndex >= args.Length)
    throw new IndexOutOfRangeException("No Discord ApplicationKey provided");
string discordKey = args[discordKeyIndex];
DisMediaRP _ = new (discordKey, level);