using System.Reflection;
using Windows.Media;
using Windows.Media.Control;
using DiscordRPC;
using GlaxLogger;
using Microsoft.Extensions.Logging;
using WindowsMediaController;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DiscordMediaRP;

//https://discord.com/developers/docs/rich-presence/how-to
//https://github.com/Lachee/discord-rpc-csharp?tab=readme-ov-file
//https://github.com/DubyaDude/WindowsMediaController
public class DisMediaRP : IDisposable
{
    private readonly ILogger? _logger;
    private readonly MediaManager _mediaManager = new();
    private readonly DiscordRpcClient _discordRpcClient;
    private readonly RichPresence _currentStatus = new()
    {
        Assets = new()
        {
            LargeImageText = "C9Glax/DiscordMediaRichPresence",
            SmallImageText = "https://www.flaticon.com/de/autoren/alfanz"
        }
    };
    private bool _running = true;

    public DisMediaRP(string applicationId, LogLevel? logLevel, string? largeImageKey = null) : this(applicationId, new Logger(logLevel ?? LogLevel.Information), largeImageKey)
    {
    }

    public DisMediaRP(string applicationId, ILogger? logger = null, string? largeImageKey = null)
    {
        this._logger = logger;
        this._currentStatus.Assets.LargeImageKey = largeImageKey ?? "cat";
        this._discordRpcClient = new DiscordRpcClient(applicationId, logger: new DisLogger(this._logger));
        this._discordRpcClient.Initialize();
        this._discordRpcClient.OnError += (sender, args) =>
        {
            this._logger?.LogError("Discord RPC encountered an error:\n{args}", args);
            this.Dispose();
        };

        this._mediaManager.Start();
        this._mediaManager.OnAnyMediaPropertyChanged += MediaPropertyChanged;
        this._mediaManager.OnAnyPlaybackStateChanged += PlaybackStateChanged;
        this._mediaManager.OnAnyTimelinePropertyChanged += TimelinePropertyChanged;


        if (this._mediaManager.GetFocusedSession() is not null)
        {
            try
            {
                this.MediaPropertyChanged(this._mediaManager.GetFocusedSession(),
                    this._mediaManager.GetFocusedSession().ControlSession.TryGetMediaPropertiesAsync().GetResults());
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                this._logger?.LogError("Could not fetch MediaProperties\n{e}", e);
            }
            this.PlaybackStateChanged(this._mediaManager.GetFocusedSession(), this._mediaManager.GetFocusedSession().ControlSession.GetPlaybackInfo());
            this.TimelinePropertyChanged(this._mediaManager.GetFocusedSession(), this._mediaManager.GetFocusedSession().ControlSession.GetTimelineProperties());
        }
        
        while(_running)
            Thread.Sleep(50);
    }

    private void MediaPropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
    {
        this._logger?.LogDebug(ObjectToString(mediaSession));
        this._logger?.LogDebug(ObjectToString(mediaProperties));
        
        string details = $"{mediaProperties.Title}";
        if (mediaProperties.Artist.Length > 0)
            details += $" - {mediaProperties.Artist}";
        if (mediaProperties.AlbumTitle.Length > 0)
            details += $" - Album: {mediaProperties.AlbumTitle}";
        this._currentStatus.Details = details;
        
        this._discordRpcClient.SetPresence(this._currentStatus);
    }

    private void PlaybackStateChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
    {
        this._logger?.LogDebug(ObjectToString(mediaSession));
        this._logger?.LogDebug(ObjectToString(playbackInfo));
        
        string playbackState = playbackInfo.PlaybackStatus switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => "pause",
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => "play",
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => "stop",
            _ => "music"
        };

        string? repeatMode = playbackInfo.AutoRepeatMode switch
        {
            MediaPlaybackAutoRepeatMode.Track => "\ud83d\udd02", 
            MediaPlaybackAutoRepeatMode.List => "\ud83d\udd01",
            _ => null
        };

        string? shuffle = (playbackInfo.IsShuffleActive ?? false)  ? "\ud83d\udd00" : null;

        this._currentStatus.State = string.Join(' ', repeatMode, shuffle);
        this._currentStatus.Assets.SmallImageKey = playbackState;
        
        this._discordRpcClient.SetPresence(this._currentStatus);
    }

    private void TimelinePropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties)
    {
        this._logger?.LogDebug(ObjectToString(mediaSession));
        this._logger?.LogDebug(ObjectToString(timelineProperties));

        if (timelineProperties.LastUpdatedTime < DateTimeOffset.UnixEpoch)
            return;
        
        this._currentStatus.Timestamps = new Timestamps(DateTime.Now.Subtract(timelineProperties.Position),
            DateTime.Now.Add(timelineProperties.EndTime - timelineProperties.Position));
        
        this._discordRpcClient.SetPresence(this._currentStatus);
    }

    public void Dispose()
    {
        _mediaManager.Dispose();
        _discordRpcClient.Dispose();
        _running = false;
    }

    private string? ObjectToString(object? obj)
    {
        if (obj is null)
            return null;
        string? ns = obj.GetType().Namespace;
        if (ns == "System.Collections.Generic")
        {
            IReadOnlyCollection<string> i = (IReadOnlyCollection<string>) obj;
            return string.Join(", ", i);
        }
        if (ns is null || (ns != "Windows.Media.Control" && ns != "WindowsMediaController"))
            return obj.ToString();
        
        FieldInfo[] fieldInfos = obj.GetType().GetFields();
        PropertyInfo[] propertyInfos = obj.GetType().GetProperties();
        return
            $"{obj.GetType().FullName}\n" +
            $"Fields:\n\t" +
            $"{string.Join("\n\t", fieldInfos.Select(f => $"{f.Name} {f.GetValue(obj)}"))}\n" +
            $"Properties:\n\t" +
            $"{string.Join("\n\t", propertyInfos.Select(p => $"{p.Name} {ObjectToString(p.GetValue(obj))}"))}";
    }
}