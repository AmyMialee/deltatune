using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace DeltaTune.Media
{
    public class SystemMediaInfoService : IMediaInfoService, IDisposable
    {
        private readonly IMediaFilter mediaFilter;

        public ConcurrentQueue<MediaInfo> UpdateQueue { get; }
        private GlobalSystemMediaTransportControlsSessionManager currentSessionManager;
        private GlobalSystemMediaTransportControlsSession currentSession;
        private MediaInfo lastMediaInfo;
        
        public SystemMediaInfoService(IMediaFilter mediaFilter)
        {
            this.mediaFilter = mediaFilter;
            UpdateQueue = new ConcurrentQueue<MediaInfo>();
            
            Task.Run(async () =>
            {
                GlobalSystemMediaTransportControlsSessionManager sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                currentSessionManager = sessionManager;
                sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                
                await Task.Run(() => OnCurrentSessionChanged(sessionManager, null));
            }).Wait();
        }
        
        public bool IsCurrentlyStopped()
        {
            if (currentSessionManager == null) return true;
            
            lock (currentSessionManager)
            {
                if (currentSessionManager == null || currentSession == null) return true;
                
                var systemPlaybackInfo = currentSession.GetPlaybackInfo();
                if (systemPlaybackInfo == null) return true;
            
                return PlaybackStatusHelper.FromSystemPlaybackStatus(systemPlaybackInfo.PlaybackStatus) == PlaybackStatus.Stopped;
            }
        }
        
        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sessionManager, CurrentSessionChangedEventArgs args)
        {
            if (currentSessionManager == null) return;
            
            lock (currentSessionManager)
            {
                if (currentSession != null)
                {
                    currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                    currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                }
                
                currentSessionManager = sessionManager;
            
                GlobalSystemMediaTransportControlsSession session = sessionManager.GetCurrentSession();
                currentSession = session;
            
                if (session != null)
                {
                    session.MediaPropertiesChanged += OnMediaPropertiesChanged;
                    OnMediaPropertiesChanged(session, null);
                    session.PlaybackInfoChanged += OnPlaybackInfoChanged;
                    OnPlaybackInfoChanged(session, null);
                }
            }
        }
        
        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            try
            {
                MediaInfo? newMediaInfo = GetCurrentMediaInfo(sender).Result;
                if (newMediaInfo != null && !newMediaInfo.Value.Equals(lastMediaInfo))
                {
                    if(!mediaFilter.Passing(newMediaInfo.Value)) return;
                    
                    UpdateQueue.Enqueue(newMediaInfo.Value);
                    lastMediaInfo = newMediaInfo.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving media properties: {ex.Message}");
            }
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            try
            {
                GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo = sender.GetPlaybackInfo();
                PlaybackStatus newStatus = PlaybackStatusHelper.FromSystemPlaybackStatus(playbackInfo.PlaybackStatus);
                if (newStatus != lastMediaInfo.Status)
                {
                    MediaInfo? newMediaInfo = GetCurrentMediaInfo(sender).Result;
                    if (newMediaInfo != null)
                    {
                        lastMediaInfo = newMediaInfo.Value;
                    }
                    
                    MediaInfo update = new MediaInfo(lastMediaInfo.Title, lastMediaInfo.Artist, newStatus);

                    if(!mediaFilter.Passing(update)) return;

                    UpdateQueue.Enqueue(update);
                    lastMediaInfo = update;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving playback info: {ex.Message}");
            }
        }

        private async Task<MediaInfo?> GetCurrentMediaInfo(GlobalSystemMediaTransportControlsSession session)
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties = await session.TryGetMediaPropertiesAsync();
            return new MediaInfo(mediaProperties.Title, mediaProperties.Artist, lastMediaInfo.Status);
        }

        public void Dispose()
        {
            if (currentSessionManager != null)
            {
                currentSessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
            }

            if (currentSession != null)
            {
                currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }
        }
    }
}