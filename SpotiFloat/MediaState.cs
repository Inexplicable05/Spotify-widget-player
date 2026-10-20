using System.Windows.Media.Imaging;

namespace SpotiFloat
{
    /// <summary>
    /// Snapshot of the current media playback state.
    /// Passed from MainWindow to whichever IPlayerControl is active.
    /// </summary>
    public class MediaState
    {
        public string Title       { get; set; } = string.Empty;
        public string Artist      { get; set; } = string.Empty;
        public BitmapSource? Art  { get; set; }
        public bool IsPlaying     { get; set; }
        public double Position    { get; set; }   // seconds
        public double Duration    { get; set; }   // seconds
        public bool IsNewTrack    { get; set; }   // true on first update for a new song
    }
}
