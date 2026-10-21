using System;

namespace SpotiFloat
{
    /// <summary>
    /// Contract all three player layouts must implement.
    /// MainWindow calls UpdateMediaState each tick; controls fire events back for commands.
    /// </summary>
    public interface IPlayerControl
    {
        // ── Media command callbacks ───────────────────────────────────────────
        event Action<double>? SeekRequested;      // seconds
        event Action?         PrevRequested;
        event Action?         PlayPauseRequested;
        event Action?         NextRequested;

        // ── Data push from MainWindow ─────────────────────────────────────────
        void UpdateMediaState(MediaState state);
        void ApplyTitleAppearance(int themeIndex, System.Windows.Media.Color? albumColour);

        // ── Scale (MainWindow drives scale via this transform) ────────────────
        System.Windows.Media.ScaleTransform? AppScaleTransform { get; }
    }
}
