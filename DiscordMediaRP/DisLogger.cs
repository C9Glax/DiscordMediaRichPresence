using Microsoft.Extensions.Logging;

namespace DiscordMediaRP;

public class DisLogger : DiscordRPC.Logging.ILogger
{
    //WHY TF DID YOU CREATE YOUR OWN LOGGER-TYPE YOU *********
    private ILogger? _logger;

    public DisLogger(ILogger? logger)
    {
        this._logger = logger;
    }

    public void Trace(string message, params object[] args)
    {
        this._logger?.Log(LogLevel.Trace, message, args);
    }

    public void Info(string message, params object[] args)
    {
        this._logger?.Log(LogLevel.Information, message, args);
    }

    public void Warning(string message, params object[] args)
    {
        this._logger?.Log(LogLevel.Warning, message, args);
    }

    public void Error(string message, params object[] args)
    {
        this._logger?.Log(LogLevel.Error, message, args);
    }

    public DiscordRPC.Logging.LogLevel Level { get; set; }
}