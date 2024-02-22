// See https://aka.ms/new-console-template for more information

using DiscordMediaRP;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

Config? c = null;
if (File.Exists("config.json"))
    c = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

int discordKeyIndex = Array.IndexOf(args, "-d");
if (discordKeyIndex > -1)
    if (discordKeyIndex + 1 < args.Length)
        if (c is null)
            c = new Config()
            {
                DiscordKey = args[discordKeyIndex + 1]
            };
        else
            c = c.Value.WithDiscordKey(args[discordKeyIndex + 1]);
    else
        throw new IndexOutOfRangeException("No Discord ApplicationKey provided");
else if(c is null)
    throw new ArgumentNullException(nameof(Config.DiscordKey));


int loglevelIndex = Array.IndexOf(args, "-l");
if(loglevelIndex > -1)
    if (loglevelIndex + 1 < args.Length)
        c = c.Value.WithLogLevel(Enum.Parse<LogLevel>(args[loglevelIndex + 1]));
    else
        throw new IndexOutOfRangeException(nameof(loglevelIndex));

int imageKeyIndex = Array.IndexOf(args, "-i");
if(imageKeyIndex > -1)
    if (imageKeyIndex + 1 < args.Length)
        c = c.Value.WithLargeImageKey(args[imageKeyIndex + 1]);
    else
        throw new IndexOutOfRangeException(nameof(imageKeyIndex));

DisMediaRP _ = new (c.Value);