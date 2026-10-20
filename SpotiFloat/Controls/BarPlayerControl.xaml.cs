using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WinInput = System.Windows.Input;

namespace SpotiFloat.Controls
{
    public partial class BarPlayerControl : PlayerControlBase
    {
        private bool   _isDragging;
        public BarPlayerControl()
        {
            InitializeComponent();
            SetAlbumArtSource(AlbumArtImage, null, false);
            UpdatePlayPauseIcon(PlayPauseIcon, false);
        }

        // ── IPlayerControl ────────────────────────────────────────────────────

        public override void UpdateMediaState(MediaState state)
        {
            base.UpdateMediaState(state);

            // Controls availability
            bool hasMedia = !string.IsNullOrEmpty(state.Title);
            BtnPrev.IsEnabled      = hasMedia;
            BtnPlayPause.IsEnabled = hasMedia;
            BtnNext.IsEnabled      = hasMedia;

            TitleText.Text  = hasMedia ? state.Title  : "No Media Available";
            ArtistText.Text = hasMedia ? state.Artist : "No media data available yet";

            // Play / Pause icon
            UpdatePlayPauseIcon(PlayPauseIcon, state.IsPlaying);

            // Album art + fade
            SetAlbumArtSource(AlbumArtImage, state.Art, state.IsNewTrack);

            // Progress
            if (!_isDragging && _duration > 0)
            {
                ProgressSlider.Maximum = _duration;
                ProgressSlider.Value   = _position;
            }
        }

        // ── Progress slider ───────────────────────────────────────────────────

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
            => _isDragging = true;

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            RequestSeek(ProgressSlider.Value);
        }

        private void ProgressSlider_MouseEnter(object sender, WinInput.MouseEventArgs e)
        {
            var hh = ProgressSlider.Template.FindName("HoverHighlight", ProgressSlider) as Border;
            if (hh != null) hh.Visibility = Visibility.Visible;
            TimestampPopup.IsOpen = true;
        }

        private void ProgressSlider_MouseLeave(object sender, WinInput.MouseEventArgs e)
        {
            var hh = ProgressSlider.Template.FindName("HoverHighlight", ProgressSlider) as Border;
            if (hh != null) hh.Visibility = Visibility.Hidden;
            TimestampPopup.IsOpen = false;
        }

        private void ProgressSlider_MouseMove(object sender, WinInput.MouseEventArgs e)
        {
            var pos = e.GetPosition(ProgressSlider);
            var hh  = ProgressSlider.Template.FindName("HoverHighlight", ProgressSlider) as Border;
            if (hh != null) hh.Width = Math.Max(0, pos.X);

            double ratio = Math.Clamp(pos.X / Math.Max(1, ProgressSlider.ActualWidth), 0, 1);
            var    t     = TimeSpan.FromSeconds(ratio * _duration);
            TimestampTooltipText.Text = FormatTimestamp(t.TotalSeconds);

            TimestampPopup.UpdateLayout();
            var child = TimestampPopup.Child as FrameworkElement;
            double w  = child?.ActualWidth ?? 30;
            TimestampPopup.HorizontalOffset = pos.X - w / 2;
            TimestampPopup.VerticalOffset   = -25;
        }
    }
}
