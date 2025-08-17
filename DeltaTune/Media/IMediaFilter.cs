namespace DeltaTune.Media
{
    public interface IMediaFilter
    {
        bool Passing(MediaInfo mediaInfo);
    }
}