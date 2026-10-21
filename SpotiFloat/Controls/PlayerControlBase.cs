using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SpotiFloat.Controls
{
    /// <summary>
    /// Abstract base for all three player layouts (Bar, Square, Disk).
    ///
    /// Owns:
    ///   • The four IPlayerControl events
    ///   • Shared playback state (_duration, _position, _isPlaying)
    ///   • Seek guard – suppresses UpdateMediaState position overwrite for 800 ms
    ///     after RequestSeek() so the UI never jumps back after seeking
    ///   • RequestSeek() – single entry-point for all seek operations
    ///   • AppScaleTransform – all layouts name their ScaleTransform "PlayerScale"
    ///   • Button click handlers (XAML wires them via C# inheritance)
    ///   • Shared artwork and animation utilities
    ///
    /// Each subclass overrides UpdateMediaState(state), calls base first to get
    /// the seek guard applied, then handles its own layout-specific UI.
    /// </summary>
    public abstract class PlayerControlBase : System.Windows.Controls.UserControl, IPlayerControl
    {
        private static readonly Lazy<ImageSource> _fallbackAlbumArt = new(CreateFallbackAlbumArt);
        private static readonly Geometry _playIconGeometry = CreateFrozenGeometry("M8,5.14V19.14L19,12.14L8,5.14Z");
        private static readonly Geometry _pauseIconGeometry = CreateFrozenGeometry("M14,19H18V5H14M6,19H10V5H6V19Z");

        // ── IPlayerControl events ─────────────────────────────────────────────

        public event Action<double>? SeekRequested;
        public event Action?         PrevRequested;
        public event Action?         PlayPauseRequested;
        public event Action?         NextRequested;

        // ── Shared playback state ─────────────────────────────────────────────

        protected double _duration;
        protected double _position;
        protected bool   _isPlaying;

        private DateTime _lastSeekTime = DateTime.MinValue;
        private double   _targetSeekPos = 0;
        private DateTime _lastUpdate = DateTime.Now;

        // ── IPlayerControl: scale (all layouts call their transform "PlayerScale") ─

        public ScaleTransform? AppScaleTransform =>
            FindName("PlayerScale") as ScaleTransform;

        // ── Seek entry-point ──────────────────────────────────────────────────
        // All subclasses MUST call this instead of invoking SeekRequested directly
        // so the seek guard timestamp is always recorded.

        protected void RequestSeek(double seconds)
        {
            _position = seconds;
            _targetSeekPos = seconds;
            _lastSeekTime = DateTime.Now;
            _lastUpdate = DateTime.Now;
            SeekRequested?.Invoke(seconds);
        }

        // ── IPlayerControl: UpdateMediaState ──────────────────────────────────
        // Base handles state fields + seek guard.
        // Subclass overrides this, calls base.UpdateMediaState(state) FIRST,
        // then updates its own layout-specific UI elements.

        public virtual void UpdateMediaState(MediaState state)
        {
            double dt = (DateTime.Now - _lastUpdate).TotalSeconds;
            _lastUpdate = DateTime.Now;

            _isPlaying = state.IsPlaying;
            _duration  = state.Duration;

            double timeSinceSeek = (DateTime.Now - _lastSeekTime).TotalSeconds;

            // Seek guard: up to 5 seconds, ignore stale Spotify API positions.
            if (timeSinceSeek < 5.0)
            {
                // If the API's position is within 2.5s of our target, the API has caught up.
                if (Math.Abs(state.Position - _targetSeekPos) < 2.5)
                {
                    _lastSeekTime = DateTime.MinValue; // cancel guard
                    _position = state.Position;
                }
                else
                {
                    // API is still stale. Interpolate position so the UI doesn't freeze.
                    if (_isPlaying)
                    {
                        _position += dt;
                        _targetSeekPos += dt;
                    }
                }
            }
            else
            {
                _position = state.Position;
            }
        }

        // ── Button click handlers ─────────────────────────────────────────────
        // Declared protected so XAML event wiring resolves them through inheritance.

        protected void BtnPrev_Click(object sender, RoutedEventArgs e)      => PrevRequested?.Invoke();
        protected void BtnPlayPause_Click(object sender, RoutedEventArgs e) => PlayPauseRequested?.Invoke();
        protected void BtnNext_Click(object sender, RoutedEventArgs e)      => NextRequested?.Invoke();

        // ── Shared utilities ──────────────────────────────────────────────────

        protected static ImageSource FallbackAlbumArt => _fallbackAlbumArt.Value;

        public void ApplyTitleAppearance(int themeIndex, System.Windows.Media.Color? albumColour)
        {
            if (FindName("TitleText") is not TextBlock title) return;
            title.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextForeground");
            title.Effect = null;
            if (themeIndex == 3 || albumColour == null) return;
            if (themeIndex == 2)
            {
                title.Foreground = new SolidColorBrush(TextContrast.AccentOn(albumColour.Value, Colors.Black));
                title.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black, ShadowDepth = 0, BlurRadius = 2, Opacity = 1
                };
                return;
            }
            if (TryFindResource("AppBackground") is not SolidColorBrush surface) return;
            var background = surface.Color;
            if (FindName("HoverGroup") != null && TryFindResource("PlayerOverlayColour") is System.Windows.Media.Color overlay)
            {
                // Test against the least favourable image beneath the translucent overlay.
                byte behind = TextContrast.ReadableOn(overlay) == Colors.White ? (byte)255 : (byte)0;
                byte Composite(byte value) => (byte)Math.Round((value * overlay.A + behind * (255 - overlay.A)) / 255.0);
                background = System.Windows.Media.Color.FromRgb(Composite(overlay.R), Composite(overlay.G), Composite(overlay.B));
            }
            title.Foreground = new SolidColorBrush(TextContrast.AccentOn(albumColour.Value, background));
        }

        /// Smoothly animates an element's Opacity to <paramref name="to"/> over
        /// <paramref name="ms"/> milliseconds with a CubicEase-Out curve.
        protected static void AnimateOpacity(UIElement? el, double to, int ms)
        {
            if (el == null) return;
            el.BeginAnimation(OpacityProperty,
                new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms)))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }

        protected static void UpdatePlayPauseIcon(Path icon, bool isPlaying)
        {
            icon.Data = isPlaying ? _pauseIconGeometry : _playIconGeometry;
        }

        protected static string FormatTimestamp(double seconds)
        {
            var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
        }

        protected static void SetAlbumArtSource(System.Windows.Controls.Image image, BitmapSource? art, bool fade)
        {
            ImageSource source = art ?? FallbackAlbumArt;
            bool changed = !Equals(image.Source, source);
            image.Source = source;

            if (fade && changed)
            {
                image.Opacity = 0;
                image.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(600)))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }
        }

        private static Geometry CreateFrozenGeometry(string data)
        {
            var geometry = Geometry.Parse(data);
            geometry.Freeze();
            return geometry;
        }

        private static ImageSource CreateFallbackAlbumArt()
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri("pack://application:,,,/icon.ico");
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(29, 185, 84));
                brush.Freeze();
                var geometry = CreateFrozenGeometry("M0,0H1V1H0Z");
                var drawing = new GeometryDrawing(brush, null, geometry);
                drawing.Freeze();
                var image = new DrawingImage(drawing);
                image.Freeze();
                return image;
            }
        }


    }
}
