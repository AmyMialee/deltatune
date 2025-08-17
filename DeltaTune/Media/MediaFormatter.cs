using System;
using System.Text;
using DeltaTune.Settings;

namespace DeltaTune.Media
{
    public class MediaFormatter : IMediaFormatter
    {
        private readonly ISettingsService settingsService;
        private readonly StringBuilder stringBuilder = new StringBuilder();

        public MediaFormatter(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public string Format(MediaInfo mediaInfo)
        {
            stringBuilder.Clear();
            
            bool hasArtist = mediaInfo.Artist != string.Empty;

            mediaInfo.Title = mediaInfo.Title.Trim();
            
            if (hasArtist)
            {
                mediaInfo.Artist = mediaInfo.Artist.Trim();
                
                // Remove YouTube's "- Topic" suffix
                if (mediaInfo.Artist.EndsWith(" - Topic"))
                {
                    mediaInfo.Artist = mediaInfo.Artist.Substring(0, mediaInfo.Artist.Length - 8);
                }

                // Remove artist prefix from the title if it exists
                if (mediaInfo.Title.StartsWith($"{mediaInfo.Artist} - "))
                {
                    mediaInfo.Title = mediaInfo.Title.Remove(0, $"{mediaInfo.Artist} - ".Length);
                }

                // Remove artist suffix from the title if it exists
                if (mediaInfo.Title.EndsWith($" - {mediaInfo.Artist}"))
                {
                    int startIndex = mediaInfo.Title.LastIndexOf($" - {mediaInfo.Artist}", StringComparison.Ordinal);
                    mediaInfo.Title = mediaInfo.Title.Remove(startIndex);
                }
            }

            if (mediaInfo.Status == PlaybackStatus.Paused && settingsService.ShowPlaybackStatus.Value)
            {
                stringBuilder.Append("⏸~   ");
            }
            else
            {
                stringBuilder.Append("♪~   ");
            }
            
            if (hasArtist && settingsService.ShowArtistName.Value)
            {
                stringBuilder.Append($"{mediaInfo.Artist} - ");
            }
            
            stringBuilder.Append(mediaInfo.Title);
            
            return stringBuilder.ToString();
        }
    }
}