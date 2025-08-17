namespace DeltaTune.Media
{
    public class MediaFilter : IMediaFilter
    {
        public bool Passing(MediaInfo mediaInfo)
        {
            // Block completely empty updates
            if (mediaInfo.Artist == string.Empty && mediaInfo.Title == string.Empty) return false;
            
            // Block Twitter videos
            if (mediaInfo.Title.EndsWith("/ X") || mediaInfo.Title.EndsWith("/ Twitter")) return false;
            
            return true;
        }
    }
}