using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WinInput = System.Windows.Input;

namespace SpotiFloat.Controls
{
    public partial class SquarePlayerControl : PlayerControlBase
    {

        // â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private bool   _isHovered;
        private bool   _isDraggingProgress;

        // â”€â”€ Visualizer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private const int  BarCount   = 28;
        private readonly System.Windows.Shapes.Rectangle[] _bars = new System.Windows.Shapes.Rectangle[BarCount];
        private readonly float[]     _smoothBands = new float[BarCount];
        private DispatcherTimer?     _vizTimer;
        private SizeChangedEventHandler? _visualizerSizeChangedHandler;

        public SquarePlayerControl()
        {
            InitializeComponent();
            Loaded   += OnLoaded;
            Unloaded += OnUnloaded;
            SetAlbumArtSource(AlbumArtImage, null, false);
            UpdatePlayPauseIcon(PlayPauseIcon, false);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildVisualizerBars();
            StartVisualizerTimer();

            if (_visualizerSizeChangedHandler == null)
            {
                _visualizerSizeChangedHandler = (_, _) => BuildVisualizerBars();
                VisualizerCanvas.SizeChanged += _visualizerSizeChangedHandler;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_visualizerSizeChangedHandler != null)
            {
                VisualizerCanvas.SizeChanged -= _visualizerSizeChangedHandler;
                _visualizerSizeChangedHandler = null;
            }

            if (_vizTimer != null)
            {
                _vizTimer.Stop();
                _vizTimer.Tick -= VizTimer_Tick;
                _vizTimer = null;
            }

            if (_isDraggingProgress)
            {
                _isDraggingProgress = false;
                Mouse.Capture(null);
            }
        }

        // â”€â”€ Visualizer setup â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void BuildVisualizerBars()
        {
            VisualizerCanvas.Children.Clear();
            // Width is resolved after layout; use 200 as initial fallback, re-built on SizeChanged
            double canvasW = VisualizerCanvas.ActualWidth > 0 ? VisualizerCanvas.ActualWidth : 200;
            double gap     = 2;
            double barW    = (canvasW - gap * (BarCount - 1)) / BarCount;

            for (int i = 0; i < BarCount; i++)
            {
                var r = new System.Windows.Shapes.Rectangle
                {
                    Width  = barW,
                    Height = 2,
                    RadiusX = 1, RadiusY = 1,
                    // Full-opacity white — contrast comes from the gradient scrim behind
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255)),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                Canvas.SetBottom(r, 0);
                Canvas.SetLeft(r, i * (barW + gap));
                VisualizerCanvas.Children.Add(r);
                _bars[i] = r;
            }
        }

        private void StartVisualizerTimer()
        {
            if (_vizTimer != null) return;
            _vizTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
            _vizTimer.Tick += VizTimer_Tick;
            _vizTimer.Start();
        }

        private void VizTimer_Tick(object? sender, EventArgs e)
        {
            // Lerp displayed bars toward audio data — larger multiplier = taller, more energetic bars
            var bands = AudioProcessor.Instance.FrequencyBands;
            double maxH = VisualizerCanvas.ActualHeight > 0 ? VisualizerCanvas.ActualHeight : 70;

            for (int i = 0; i < BarCount; i++)
            {
                _smoothBands[i] += (bands[i] - _smoothBands[i]) * 0.28f; // faster response
                double h = Math.Max(2, _smoothBands[i] * maxH * 1.5);    // 1.5x amplitude
                _bars[i].Height = h;
                Canvas.SetBottom(_bars[i], 0);
            }

            UpdateIdleProgressFill();
        }

        private void UpdateIdleProgressFill()
        {
            double idleWidth = IdleProgressContainer.ActualWidth > 0 ? IdleProgressContainer.ActualWidth : 240;
            double hoverWidth = HoverProgressContainer.ActualWidth > 0 ? HoverProgressContainer.ActualWidth : idleWidth;
            if (_duration <= 0)
            {
                IdleProgressFill.Width = 0;
                HoverProgressFill.Width = 0;
                HoverProgressKnobMargin(0);
                return;
            }

            double ratio = Math.Clamp(_position / _duration, 0, 1);
            double idleFillWidth = idleWidth * ratio;
            IdleProgressFill.Width = idleFillWidth;

            if (_isHovered)
            {
                double hoverFillWidth = hoverWidth * ratio;
                HoverProgressFill.Width  = hoverFillWidth;
                double knobL = Math.Clamp(hoverFillWidth - 7, 0, Math.Max(0, hoverWidth - 14));
                HoverProgressKnobMargin(knobL);
            }
        }

        private void HoverProgressKnobMargin(double left)
        {
            ProgressKnob.Margin = new Thickness(left, 0, 0, 0);
        }

        // â”€â”€ Hover transitions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void OnMouseEnter(object sender, WinInput.MouseEventArgs e)
        {
            ShowHover();
        }

        private void OnMouseLeave(object sender, WinInput.MouseEventArgs e)
        {
            if (_isDraggingProgress) return;
            HideHover();
        }

        private void ShowHover()
        {
            _isHovered = true;
            HoverGroup.IsHitTestVisible = true;
            AlbumArtBlur.Source = AlbumArtImage.Source ?? FallbackAlbumArt;

            AnimateOpacity(VisualizerCanvas, 0, 200);
            AnimateOpacity(AlbumArtBlur, 1, 200);
            AnimateOpacity(HoverGroup, 1, 200);
            UpdateIdleProgressFill();
        }

        private void HideHover()
        {
            _isHovered = false;
            HoverGroup.IsHitTestVisible = false;

            AnimateOpacity(VisualizerCanvas, 1, 200);
            AnimateOpacity(AlbumArtBlur, 0, 200);
            AnimateOpacity(HoverGroup, 0, 200);
            TimestampBadge.Visibility = Visibility.Collapsed;
        }

        // â”€â”€ IPlayerControl.UpdateMediaState â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public override void UpdateMediaState(MediaState state)
        {
            base.UpdateMediaState(state);

            TitleText.Text  = string.IsNullOrWhiteSpace(state.Title) ? "No Media Available" : state.Title;
            ArtistText.Text = string.IsNullOrWhiteSpace(state.Artist) ? "No media data available yet" : state.Artist;

            UpdatePlayPauseIcon(PlayPauseIcon, state.IsPlaying);

            SetAlbumArtSource(AlbumArtImage, state.Art, state.IsNewTrack);
            if (_isHovered)
                AlbumArtBlur.Source = AlbumArtImage.Source ?? FallbackAlbumArt;

            UpdateIdleProgressFill();
        }

        // â”€â”€ Progress bar (hover, 10px, seekable) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void ProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;          // prevent Window_MouseDown → DragMove() from firing
            _isDraggingProgress = true;
            ((UIElement)sender).CaptureMouse();
            SeekToMouseX(e.GetPosition(HoverProgressContainer).X);
        }

        private void ProgressBar_MouseMove(object sender, WinInput.MouseEventArgs e)
        {
            double x = e.GetPosition(HoverProgressContainer).X;
            double ratio = Math.Clamp(x / Math.Max(1, HoverProgressContainer.ActualWidth), 0, 1);
            var t = TimeSpan.FromSeconds(ratio * _duration);

            // Show timestamp badge
            TimestampBadge.Visibility = Visibility.Visible;
            TimestampText.Text        = FormatTimestamp(t.TotalSeconds) + " / " + FormatTimestamp(_duration);
            double width = HoverProgressContainer.ActualWidth > 0 ? HoverProgressContainer.ActualWidth : 240;
            double left = Math.Clamp(x - 36, 4, Math.Max(4, width - 84));
            TimestampBadge.Margin = new Thickness(left, 0, 0, 14);

            if (_isDraggingProgress)
                SeekToMouseX(x);
        }

        private void ProgressBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingProgress)
            {
                SeekToMouseX(e.GetPosition(HoverProgressContainer).X);
                ((UIElement)sender).ReleaseMouseCapture();
                _isDraggingProgress = false;

                if (!RootGrid.IsMouseOver)
                    HideHover();
            }
        }

        private void SeekToMouseX(double x)
        {
            if (_duration <= 0) return;
            double ratio = Math.Clamp(x / Math.Max(1, HoverProgressContainer.ActualWidth), 0, 1);
            double secs  = ratio * _duration;
            RequestSeek(secs);
            UpdateIdleProgressFill();
        }
    }
}
