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
using WpfMedia = System.Windows.Media;
using WpfPoint = System.Windows.Point;

namespace SpotiFloat.Controls
{
    public partial class DiskPlayerControl : PlayerControlBase
    {

        // â”€â”€ Constants â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private const double DiscDiameter   = 220;
        private const double OuterDiameter  = 240;
        private const double ArcIdleStroke  = 3;
        private const double ArcHoverStroke = 4;
        private const double KnobSize       = 10;
        private const double ArcRadius      = DiscDiameter / 2 + 2 + ArcHoverStroke / 2;

        // â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private bool   _isHovered;
        private bool   _isDraggingArc;
        private WpfMedia.Color  _ringColor = WpfMedia.Color.FromRgb(29, 185, 84); // default green

        // â”€â”€ Arc drawing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private Path?   _arcTrack;
        private Path?   _arcFill;
        private Ellipse? _arcKnob;

        // â”€â”€ Spin animation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private DoubleAnimation? _spinAnim;
        private double           _lastAngle;

        // â”€â”€ Waveform â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private Polyline? _waveform;
        private readonly float[] _smoothWave = new float[AudioProcessor.WaveformCount];

        // â”€â”€ Visualizer timer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private DispatcherTimer? _vizTimer;
        private DateTime         _lastTick = DateTime.Now;

        public DiskPlayerControl()
        {
            InitializeComponent();
            Loaded   += OnLoaded;
            Unloaded += OnUnloaded;
            SetAlbumArtSource(AlbumArtImage, null, false);
            UpdatePlayPauseIcon(PlayPauseIcon, false);
        }

        // â”€â”€ Initialisation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildArc();
            BuildWaveform();
            StartTimer();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_vizTimer != null)
            {
                _vizTimer.Stop();
                _vizTimer.Tick -= VizTimer_Tick;
                _vizTimer = null;
            }

            if (_isDraggingArc)
            {
                _isDraggingArc = false;
                RootGrid.ReleaseMouseCapture();
            }

            HideHover();
        }

        private void BuildArc()
        {
            ArcCanvas.Children.Clear();

            double cx = OuterDiameter / 2, cy = OuterDiameter / 2;
            double r  = ArcRadius;

            // Track (dim full circle)
            _arcTrack = new Path
            {
                Data            = BuildArcGeometry(cx, cy, r, 0, 359.99),
                Stroke          = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 255, 255, 255)),
                StrokeThickness = ArcIdleStroke,
                Fill            = System.Windows.Media.Brushes.Transparent,
                IsHitTestVisible = false
            };
            ArcCanvas.Children.Add(_arcTrack);

            // Fill (bright portion = played)
            _arcFill = new Path
            {
                Stroke          = new SolidColorBrush(_ringColor),
                StrokeThickness = ArcIdleStroke,
                Fill            = System.Windows.Media.Brushes.Transparent,
                IsHitTestVisible = false
            };
            ArcCanvas.Children.Add(_arcFill);

            // Knob (hidden until hover)
            _arcKnob = new Ellipse
            {
                Width   = KnobSize, Height = KnobSize,
                Fill    = System.Windows.Media.Brushes.White,
                Opacity = 0,
                IsHitTestVisible = false
            };
            _arcKnob.Effect = new System.Windows.Media.Effects.DropShadowEffect
                { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.5 };
            ArcCanvas.Children.Add(_arcKnob);
        }

        private void BuildWaveform()
        {
            WaveformCanvas.Children.Clear();

            _waveform = new Polyline
            {
                Stroke          = new SolidColorBrush(System.Windows.Media.Color.FromArgb(115, 255, 255, 255)),
                StrokeThickness = 1.8,
                StrokeLineJoin  = PenLineJoin.Round,
                IsHitTestVisible = false
            };
            WaveformCanvas.Children.Add(_waveform);
        }

        // â”€â”€ Timer: visualizer + arc update â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void StartTimer()
        {
            if (_vizTimer != null) return;
            _vizTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _vizTimer.Tick += VizTimer_Tick;
            _vizTimer.Start();
        }

        private void VizTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            double dt = (now - _lastTick).TotalSeconds;
            _lastTick = now;

            AudioProcessor.Instance.IsPlaying = _isPlaying;
            if (!AudioProcessor.Instance.IsCapturingReal)
                AudioProcessor.Instance.TickSimulation(dt);

            UpdateWaveform();
            UpdateArc();
        }

        // â”€â”€ Waveform rendering â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void UpdateWaveform()
        {
            if (_waveform == null) return;
            var pts   = new PointCollection();
            double w  = WaveformCanvas.ActualWidth  > 0 ? WaveformCanvas.ActualWidth  : 154;
            double h  = WaveformCanvas.ActualHeight > 0 ? WaveformCanvas.ActualHeight : 60;
            double cy = h / 2;
            double amp = _isHovered ? 0 : 22; // collapse when controls visible

            var wave = AudioProcessor.Instance.WaveformPoints;
            for (int i = 0; i < AudioProcessor.WaveformCount; i++)
            {
                _smoothWave[i] += (wave[i] - _smoothWave[i]) * 0.18f;
                double x = i * w / (AudioProcessor.WaveformCount - 1);
                double y = cy + _smoothWave[i] * amp;
                pts.Add(new System.Windows.Point(x, y));
            }
            _waveform.Points = pts;
        }

        // â”€â”€ Arc rendering â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void UpdateArc()
        {
            if (_arcFill == null || _arcTrack == null || _arcKnob == null) return;
            double cx = OuterDiameter / 2, cy = OuterDiameter / 2;
            double r  = ArcRadius;

            double stroke = _isHovered ? ArcHoverStroke : ArcIdleStroke;
            _arcTrack.StrokeThickness = stroke;
            _arcFill.StrokeThickness  = stroke;

            double ratio = _duration > 0 ? Math.Clamp(_position / _duration, 0, 1) : 0;
            double sweepDeg = ratio * 359.99;

            _arcFill.Data = sweepDeg < 0.5
                ? new PathGeometry()
                : BuildArcGeometry(cx, cy, r, 0, sweepDeg);

            // Glow ring color
            (_arcFill.Stroke as SolidColorBrush)!.Color = _ringColor;

            // Knob position
            double knobAngleRad = (sweepDeg - 90) * Math.PI / 180;
            double kx = cx + r * Math.Cos(knobAngleRad) - KnobSize / 2;
            double ky = cy + r * Math.Sin(knobAngleRad) - KnobSize / 2;
            Canvas.SetLeft(_arcKnob, kx);
            Canvas.SetTop(_arcKnob, ky);

            // Animate knob opacity
            _arcKnob.Opacity = _isHovered
                ? Math.Min(1, _arcKnob.Opacity + 0.15)
                : Math.Max(0, _arcKnob.Opacity - 0.15);
        }

        private static Geometry BuildArcGeometry(double cx, double cy, double r, double startDeg, double sweepDeg)
        {
            // 12 o'clock = -90Â° offset
            double startRad = (startDeg - 90) * Math.PI / 180;
            double endRad   = (startDeg + sweepDeg - 90) * Math.PI / 180;

            double x1 = cx + r * Math.Cos(startRad);
            double y1 = cy + r * Math.Sin(startRad);
            double x2 = cx + r * Math.Cos(endRad);
            double y2 = cy + r * Math.Sin(endRad);

            bool isLargeArc = sweepDeg > 180;

            var seg = new ArcSegment(new System.Windows.Point(x2, y2), new System.Windows.Size(r, r), 0, isLargeArc,
                SweepDirection.Clockwise, true);
            var fig = new PathFigure(new System.Windows.Point(x1, y1), new[] { seg }, false);
            return new PathGeometry(new[] { fig });
        }

        // â”€â”€ Disc rotation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void StartSpin()
        {
            _spinAnim = new DoubleAnimation
            {
                From = _lastAngle, To = _lastAngle + 360,
                Duration         = TimeSpan.FromSeconds(120),
                RepeatBehavior   = RepeatBehavior.Forever
            };
            AlbumRotate.BeginAnimation(RotateTransform.AngleProperty, _spinAnim);
        }

        private void StopSpin()
        {
            _lastAngle = AlbumRotate.Angle;
            AlbumRotate.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(_lastAngle, new Duration(TimeSpan.FromMilliseconds(1500)))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }

        // â”€â”€ Hover â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void OnMouseEnter(object sender, WinInput.MouseEventArgs e)
        {
            ShowHover();
        }

        private void OnMouseLeave(object sender, WinInput.MouseEventArgs e)
        {
            if (_isDraggingArc) return;
            HideHover();
        }

        private void ShowHover()
        {
            _isHovered = true;
            HoverGroup.IsHitTestVisible = true;
            AnimateOpacity(HoverOverlay, 1, 180);
            AnimateOpacity(HoverGroup,   1, 180);
            AnimateOpacity(WaveformCanvas, 0, 180);
        }

        private void HideHover()
        {
            _isHovered = false;
            HoverGroup.IsHitTestVisible = false;
            AnimateOpacity(HoverOverlay,   0, 180);
            AnimateOpacity(HoverGroup,     0, 180);
            AnimateOpacity(WaveformCanvas, 1, 180);
            TimestampBadge.Visibility = Visibility.Collapsed;
        }

        // â”€â”€ Circular progress hit-testing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void ArcCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(RootGrid);
            if (!IsOnArcHitZone(pos)) return;
            e.Handled = true;          // prevent Window_MouseDown → DragMove() from firing
            _isDraggingArc = true;
            RootGrid.CaptureMouse();
            SeekToAngle(pos);
        }

        private void ArcCanvas_MouseMove(object sender, WinInput.MouseEventArgs e)
        {
            var pos = e.GetPosition(RootGrid);
            if (IsOnArcHitZone(pos))
                RootGrid.Cursor = System.Windows.Input.Cursors.Hand;
            else
                RootGrid.Cursor = System.Windows.Input.Cursors.Arrow;

            if (_isDraggingArc)
            {
                SeekToAngle(pos);
                UpdateTimestampBadge(pos);
            }
        }

        private void ArcCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingArc)
            {
                RootGrid.ReleaseMouseCapture();
                _isDraggingArc = false;
                TimestampBadge.Visibility = Visibility.Collapsed;

                if (!RootGrid.IsMouseOver)
                    HideHover();
            }
        }

        private bool IsOnArcHitZone(WpfPoint pos)
        {
            double cx = OuterDiameter / 2, cy = OuterDiameter / 2;
            double r  = OuterDiameter / 2;
            double d  = Math.Sqrt((pos.X - cx) * (pos.X - cx) + (pos.Y - cy) * (pos.Y - cy));
            // 16px wide band around the arc
            return d >= r - 16 && d <= r;
        }

        private void SeekToAngle(WpfPoint pos)
        {
            if (_duration <= 0) return;

            double cx = OuterDiameter / 2, cy = OuterDiameter / 2;
            double angle = Math.Atan2(pos.Y - cy, pos.X - cx) * 180 / Math.PI;
            // 12 o'clock = 0, clockwise
            double normalized = ((angle + 90) % 360 + 360) % 360;
            double ratio = normalized / 360.0;
            RequestSeek(ratio * _duration);
        }

        private void UpdateTimestampBadge(WpfPoint pos)
        {
            TimestampBadge.Visibility = Visibility.Visible;
            TimestampText.Text = FormatTimestamp(_position)
                               + " / "
                               + FormatTimestamp(_duration);

            TimestampBadge.UpdateLayout();
            double badgeWidth = TimestampBadge.ActualWidth > 0 ? TimestampBadge.ActualWidth : 72;
            double badgeHeight = TimestampBadge.ActualHeight > 0 ? TimestampBadge.ActualHeight : 22;
            TimestampBadge.Margin = new Thickness(
                Math.Clamp(pos.X - badgeWidth / 2, 4, OuterDiameter - badgeWidth - 4),
                Math.Clamp(pos.Y - badgeHeight - 8, 4, OuterDiameter - badgeHeight - 4),
                0, 0);
        }

        // â”€â”€ IPlayerControl.UpdateMediaState â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public override void UpdateMediaState(MediaState state)
        {
            bool wasPlaying = _isPlaying;
            base.UpdateMediaState(state);

            TitleText.Text  = string.IsNullOrWhiteSpace(state.Title) ? "No Media Available" : state.Title;
            ArtistText.Text = string.IsNullOrWhiteSpace(state.Artist) ? "No media data available yet" : state.Artist;

            UpdatePlayPauseIcon(PlayPauseIcon, state.IsPlaying);

            // Spin control
            if (_isPlaying && !wasPlaying) StartSpin();
            else if (!_isPlaying && wasPlaying) StopSpin();

            if (state.Art != null && (state.IsNewTrack || AlbumArtImage.Source == null))
                _ringColor = ExtractDominantColor(state.Art);
            else if (state.Art == null)
                _ringColor = WpfMedia.Color.FromRgb(29, 185, 84);

            SetAlbumArtSource(AlbumArtImage, state.Art, state.IsNewTrack);

            if (_arcFill != null)
            {
                (_arcFill.Stroke as SolidColorBrush)!.Color = _ringColor;
                UpdateArc();
            }
        }
    }
}
