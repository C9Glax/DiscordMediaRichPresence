using System.Diagnostics;
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
    private RichPresence _currentStatus;
    private bool _running = true;
    private Config config;

    private static RichPresence DefaultPresence(string? largeImageKey)
    {
        return new RichPresence()
        {
            Details = "hewwo :3",
            State = "https://github.com/C9Glax/DiscordMediaRichPresence",
            Assets = new()
            {
                LargeImageKey = largeImageKey ?? "",
                SmallImageKey = "music",
                LargeImageText = "C9Glax/DiscordMediaRichPresence",
                SmallImageText = "https://www.flaticon.com/de/autoren/alfanz"
            }
        };
    }

    public DisMediaRP(Config config)
    {
        this.config = config;
        this._logger = new Logger(config.LogLevel ?? LogLevel.Information);
        this._currentStatus = DefaultPresence(config.LargeImageKey);
        this._discordRpcClient = new DiscordRpcClient(config.DiscordKey, logger: new DisLogger(this._logger));
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
        this._mediaManager.OnFocusedSessionChanged += mediaSession =>
        {
            if (mediaSession is null)
            {
                this._currentStatus = DefaultPresence(config.LargeImageKey);
            }

            this._discordRpcClient.SetPresence(this._currentStatus);
        };


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
        }else
            this._discordRpcClient.SetPresence(this._currentStatus);
        
        while(_running)
            Thread.Sleep(50);
    }

    private void MediaPropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
    {
        this._logger?.LogDebug(ObjectToString(mediaSession));
        this._logger?.LogDebug(ObjectToString(mediaProperties));
        
        if (!UseMediaSession(mediaSession))
            return;
        
        string details = $"{mediaProperties.Title}";
        if (mediaProperties.Artist.Length > 0)
            details += $" - {mediaProperties.Artist}";
        if (mediaProperties.AlbumTitle.Length > 0)
            details += $" - Album: {mediaProperties.AlbumTitle}";
        this._currentStatus.Details = details;
        
        this.PlaybackStateChanged(mediaSession, mediaSession.ControlSession.GetPlaybackInfo());
    }

    private void PlaybackStateChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
    {
        this._logger?.LogDebug(ObjectToString(mediaSession));
        this._logger?.LogDebug(ObjectToString(playbackInfo));
        
        if (!UseMediaSession(mediaSession))
            return;
        
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

        if (!UseMediaSession(mediaSession))
            return;

        if (timelineProperties.LastUpdatedTime < DateTimeOffset.UnixEpoch)
            return;

        GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo =
            mediaSession.ControlSession.GetPlaybackInfo();
        
        string? repeatMode = playbackInfo.AutoRepeatMode switch
        {
            MediaPlaybackAutoRepeatMode.Track => "\ud83d\udd02", 
            MediaPlaybackAutoRepeatMode.List => "\ud83d\udd01",
            _ => null
        };

        string? shuffle = (playbackInfo.IsShuffleActive ?? false)  ? "\ud83d\udd00" : null;
        
        this._currentStatus.State = string.Join(' ', repeatMode, shuffle, $"{timelineProperties.Position:hh\\:mm\\:ss}/{timelineProperties.EndTime:hh\\:mm\\:ss}");

        if (mediaSession.ControlSession.GetPlaybackInfo().PlaybackStatus is
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            this._currentStatus.Timestamps = new Timestamps()
            {
                End = DateTime.UtcNow.Add(timelineProperties.EndTime - timelineProperties.Position)
            };
        else
            this._currentStatus.Timestamps = new Timestamps();
        
        this._discordRpcClient.SetPresence(this._currentStatus);
    }

    private bool UseMediaSession(MediaManager.MediaSession mediaSession)
    {
        string processId = mediaSession.ControlSession.SourceAppUserModelId;
        if (processId == "spotify.exe")
            return config.UseSpotify ?? true;

        if (processId == "firefox.exe")
        {
            string[] windowNames = Process.GetProcesses().Where(proc => processId.Contains(proc.ProcessName, StringComparison.InvariantCultureIgnoreCase)).Select(proc => proc.MainWindowTitle).ToArray();
            return !windowNames.Any(name =>
            {
                foreach (string site in config.WebbrowserIgnoreSites ?? Array.Empty<string>())
                    if (name.Contains(site, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                return false;
            });
        }

        return true;
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