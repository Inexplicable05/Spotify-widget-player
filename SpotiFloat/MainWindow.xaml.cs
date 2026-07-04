using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace SpotiFloat
{
    public enum WidgetZOrder
    {
        Topmost,
        Desktop
    }

    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;

        public WidgetZOrder ZOrder { get; set; } = WidgetZOrder.Topmost;
        public bool HideOnFullscreen { get; set; } = false;
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        private DispatcherTimer _timer;
        private bool _isDraggingSlider = false;

        public bool IsLocked { get; set; } = false;
        public int ThemeIndex { get; set; } = 0;           // 0=Dark, 1=Light, 2=Glass, 3=Dynamic
        public bool EnableDynamicTitleColour { get; set; } = true;
        public BitmapSource? CurrentAlbumArt => AlbumArtImage.Source as BitmapSource;
        private DispatcherTimer _processCheckTimer;
        private SettingsWindow? _settingsWindow;

        private bool _isSpotifyRunning = false;
        private DateTime _lastSeekTime = DateTime.MinValue;
        private string _lastTrackTitle = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            string? exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                TrayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;
            
            _processCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _processCheckTimer.Tick += ProcessCheckTimer_Tick;
            _processCheckTimer.Start();

            Loaded += MainWindow_Loaded;
        }

        private void ProcessCheckTimer_Tick(object? sender, EventArgs e)
        {
            _isSpotifyRunning = Process.GetProcessesByName("Spotify").Length > 0;
            UpdateVisibility();
        }

        private bool IsFullscreenAppActive()
        {
            IntPtr fgWnd = GetForegroundWindow();
            if (fgWnd == IntPtr.Zero) return false;

            var className = new System.Text.StringBuilder(256);
            GetClassName(fgWnd, className, className.Capacity);
            if (className.ToString() == "WorkerW" || className.ToString() == "Progman")
                return false;

            if (GetWindowRect(fgWnd, out RECT rect))
            {
                var screen = System.Windows.Forms.Screen.FromHandle(fgWnd);
                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;
                if (w >= screen.Bounds.Width && h >= screen.Bounds.Height)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateVisibility()
        {
            bool shouldBeVisible = _isSpotifyRunning;

            if (shouldBeVisible && ZOrder == WidgetZOrder.Topmost && HideOnFullscreen)
            {
                if (IsFullscreenAppActive())
                    shouldBeVisible = false;
            }

            if (shouldBeVisible && this.Visibility != Visibility.Visible)
            {
                this.Visibility = Visibility.Visible;
            }
            else if (!shouldBeVisible && this.Visibility == Visibility.Visible)
            {
                this.Visibility = Visibility.Hidden;
                if (!_isSpotifyRunning) _settingsWindow?.Close();
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_manager != null)
                {
                    _manager.CurrentSessionChanged += Manager_CurrentSessionChanged;
                    UpdateCurrentSession();
                }
            }
            catch (Exception)
            {
                // Status mapping removed
            }
        }

        private void Manager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            Dispatcher.Invoke(UpdateCurrentSession);
        }

        private void UpdateCurrentSession()
        {
            if (_manager == null) return;
            
            var newSession = _manager.GetCurrentSession();
            if (_currentSession != newSession)
            {
                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                }
                
                _currentSession = newSession;
                
                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged += Session_PlaybackInfoChanged;
                }
            }
            
            UpdateMediaState();
            
            if (_currentSession != null)
                _timer.Start();
            else
                _timer.Stop();
        }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            Dispatcher.Invoke(UpdateMediaState);
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            Dispatcher.Invoke(UpdateMediaState);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateVisibility();
            EnforceZOrder();
            UpdateMediaState();
        }

        private void EnforceZOrder()
        {
            if (this.Visibility != Visibility.Visible) return;
            
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            if (ZOrder == WidgetZOrder.Topmost)
            {
                if (!this.Topmost) this.Topmost = true;
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            else if (ZOrder == WidgetZOrder.Desktop)
            {
                if (this.Topmost) this.Topmost = false;
                SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

        private async void UpdateMediaState()
        {
            if (_currentSession == null)
            {
                TitleText.Text = "No Media Available";
                ArtistText.Text = "No media data available yet";
                AlbumArtImage.Source = new BitmapImage(new Uri("pack://application:,,,/icon.ico"));
                ProgressSlider.Value = 0;
                BtnPrev.IsEnabled = false;
                BtnPlayPause.IsEnabled = false;
                BtnNext.IsEnabled = false;
                return;
            }

            try
            {
                var properties = await _currentSession.TryGetMediaPropertiesAsync();
                TitleText.Text = properties?.Title ?? "Unknown Title";
                ArtistText.Text = properties?.Artist ?? "Unknown Artist";

                if (properties?.Thumbnail != null)
                {
                    using var stream = await properties.Thumbnail.OpenReadAsync();
                    var buffer = new Windows.Storage.Streams.Buffer((uint)stream.Size);
                    await stream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None);
                    using var dataReader = DataReader.FromBuffer(buffer);
                    byte[] bytes = new byte[buffer.Length];
                    dataReader.ReadBytes(bytes);

                    using var ms = new MemoryStream(bytes);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();

                    // --- Feature: Album-Cover Fade-In (always active) ---
                    bool isNewTrack = (TitleText.Text != _lastTrackTitle);
                    _lastTrackTitle = TitleText.Text;

                    AlbumArtImage.Source = bitmap;
                    if (isNewTrack)
                    {
                        AlbumArtImage.Opacity = 0;
                        var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1,
                            new System.Windows.Duration(TimeSpan.FromMilliseconds(600)))
                        {
                            EasingFunction = new System.Windows.Media.Animation.CubicEase
                            { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                        };
                        AlbumArtImage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    }

                    // --- Feature: Dynamic Theme (ThemeIndex 3) or Dynamic Title Colour ---
                    if (ThemeIndex == 3)
                    {
                        // Dynamic theme manages ALL colours including title; apply full palette
                        ApplyDynamicTheme(bitmap);
                    }
                    else if (EnableDynamicTitleColour)
                    {
                        // Only the title colour follows the album art
                        TitleText.Foreground = new SolidColorBrush(GetDominantColor(bitmap));
                    }
                    else
                    {
                        TitleText.SetResourceReference(
                            System.Windows.Controls.TextBlock.ForegroundProperty,
                            "PrimaryTextForeground");
                    }
                }
                else
                {
                    AlbumArtImage.Source = new BitmapImage(new Uri("pack://application:,,,/icon.ico"));
                    TitleText.SetResourceReference(
                        System.Windows.Controls.TextBlock.ForegroundProperty,
                        "PrimaryTextForeground");
                }
            }
            catch { }

            var playbackInfo = _currentSession.GetPlaybackInfo();
            if (playbackInfo != null)
            {
                var isPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                
                if (isPlaying)
                {
                    PlayPauseIcon.Data = Geometry.Parse("M14,19H18V5H14M6,19H10V5H6V19Z");
                }
                else
                {
                    PlayPauseIcon.Data = Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z");
                }

                BtnPlayPause.IsEnabled = true;
                BtnPrev.IsEnabled = true;
                BtnNext.IsEnabled = true;
            }
            
            var timeline = _currentSession.GetTimelineProperties();
            if (timeline != null && !_isDraggingSlider && (DateTime.Now - _lastSeekTime).TotalSeconds > 2)
            {
                ProgressSlider.Minimum = 0;
                ProgressSlider.Maximum = timeline.EndTime.TotalSeconds > 0 ? timeline.EndTime.TotalSeconds : 100;
                ProgressSlider.Value = timeline.Position.TotalSeconds;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !IsLocked)
                this.DragMove();
        }

        private async void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession != null)
                await _currentSession.TrySkipPreviousAsync();
        }

        private async void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession != null)
                await _currentSession.TryTogglePlayPauseAsync();
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession != null)
                await _currentSession.TrySkipNextAsync();
        }

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private async void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = false;
            _lastSeekTime = DateTime.Now;
            if (_currentSession != null)
            {
                long ticks = (long)(ProgressSlider.Value * 10000000);
                await _currentSession.TryChangePlaybackPositionAsync(ticks);
            }
        }

        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        public void OpenSettings()
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow(this);
                _settingsWindow.Show();
            }
            else
            {
                _settingsWindow.Activate();
            }
        }

        public void RefreshMedia()
        {
            UpdateCurrentSession();
        }
        private void ProgressSlider_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_currentSession == null) return;
            var hoverHighlight = ProgressSlider.Template.FindName("HoverHighlight", ProgressSlider) as System.Windows.Controls.Border;
            if (hoverHighlight != null) hoverHighlight.Visibility = Visibility.Visible;
            TimestampPopup.IsOpen = true;
        }

        private void ProgressSlider_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var hoverHighlight = ProgressSlider.Template.FindName("HoverHighlight", ProgressSlider) as System.Windows.Controls.Border;
            if (hoverHighlight != null) hoverHighlight.Visibility = Visibility.Hidden;
            TimestampPopup.IsOpen = false;
        }

        private void ProgressSlider_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_currentSession == null) return;
            
            var pos = e.GetPosition(ProgressSlider);
            var hoverHighlight = ProgressSlider.Template.FindName("HoverHighlight", ProgressSlider) as System.Windows.Controls.Border;
            
            if (hoverHighlight != null)
            {
                hoverHighlight.Width = Math.Max(0, pos.X);
            }

            var ratio = pos.X / ProgressSlider.ActualWidth;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            var playbackInfo = _currentSession.GetTimelineProperties();
            if (playbackInfo != null)
            {
                var duration = playbackInfo.EndTime.Subtract(playbackInfo.StartTime);
                var hoverTime = TimeSpan.FromTicks((long)(duration.Ticks * ratio));
                TimestampTooltipText.Text = hoverTime.ToString(@"m\:ss");

                // Position the popup overlay
                TimestampPopup.UpdateLayout();
                var childBorder = TimestampPopup.Child as System.Windows.FrameworkElement;
                double width = childBorder != null ? childBorder.ActualWidth : 30;
                TimestampPopup.HorizontalOffset = pos.X - (width / 2);
                TimestampPopup.VerticalOffset = -25;
            }
        }
        // ──────────────────────────────────────────────────────────────────────
        // Dynamic Theme — Adaptive Contrast Palette
        // ──────────────────────────────────────────────────────────────────────

        // Keys we override when Dynamic theme is active; cleared on switch to static themes.
        private static readonly string[] DynamicThemeKeys =
        {
            "AppBackground", "AppBorder", "PrimaryTextForeground", "SecondaryTextForeground",
            "ButtonBackground", "ButtonIconForeground",
            "SliderTrackBackground", "SliderTrackHover", "SliderThumb",
            "AlbumArtBackground", "AlbumArtBorder",
            "TooltipBackground", "TooltipForeground",
            "WindowShadowOpacity"
        };

        /// <summary>Removes all dynamic-theme overrides so a static XAML theme can take effect.</summary>
        public static void ClearDynamicThemeOverrides()
        {
            var res = System.Windows.Application.Current.Resources;
            foreach (var key in DynamicThemeKeys)
                if (res.Contains(key)) res.Remove(key);
        }

        /// <summary>
        /// Derives a full, contrast-safe colour palette from the album art and applies
        /// it to Application.Current.Resources, overriding any merged XAML theme.
        /// </summary>
        public void ApplyDynamicTheme(BitmapSource source)
        {
            System.Windows.Media.Color seed = GetDominantColor(source);

            // Slightly desaturate toward dark so the widget isn't overwhelming
            byte sR = (byte)(seed.R * 0.72 + 18);
            byte sG = (byte)(seed.G * 0.72 + 18);
            byte sB = (byte)(seed.B * 0.72 + 18);
            System.Windows.Media.Color surface = System.Windows.Media.Color.FromRgb(sR, sG, sB);

            // WCAG relative luminance → decide if white or near-black foreground
            double L = 0.2126 * (sR / 255.0) + 0.7152 * (sG / 255.0) + 0.0722 * (sB / 255.0);
            bool isDark = L < 0.45;

            System.Windows.Media.Color onSurface   = isDark
                ? System.Windows.Media.Color.FromRgb(240, 240, 240)
                : System.Windows.Media.Color.FromRgb(15, 15, 15);
            System.Windows.Media.Color secondaryFg = isDark
                ? System.Windows.Media.Color.FromRgb(190, 190, 190)
                : System.Windows.Media.Color.FromRgb(80, 80, 80);

            // Surface variant: button / element backgrounds (slightly offset from surface)
            System.Windows.Media.Color surfaceVar = isDark
                ? System.Windows.Media.Color.FromRgb(Clamp(sR + 30, 0, 255), Clamp(sG + 30, 0, 255), Clamp(sB + 30, 0, 255))
                : System.Windows.Media.Color.FromRgb(Clamp(sR - 25, 0, 255), Clamp(sG - 25, 0, 255), Clamp(sB - 25, 0, 255));

            // Slider & border colours as alpha-blends
            System.Windows.Media.Color borderColor    = System.Windows.Media.Color.FromArgb(isDark ? (byte)70  : (byte)50,  0, 0, 0);
            System.Windows.Media.Color sliderTrack    = System.Windows.Media.Color.FromArgb(isDark ? (byte)55  : (byte)45,  onSurface.R, onSurface.G, onSurface.B);
            System.Windows.Media.Color sliderHover    = System.Windows.Media.Color.FromArgb(isDark ? (byte)110 : (byte)90,  onSurface.R, onSurface.G, onSurface.B);
            System.Windows.Media.Color albumArtBorder = System.Windows.Media.Color.FromArgb(isDark ? (byte)80  : (byte)60,  onSurface.R, onSurface.G, onSurface.B);
            System.Windows.Media.Color tooltipBg      = System.Windows.Media.Color.FromRgb(
                Clamp(isDark ? sR + 40 : sR - 35, 0, 255),
                Clamp(isDark ? sG + 40 : sG - 35, 0, 255),
                Clamp(isDark ? sB + 40 : sB - 35, 0, 255));

            // Apply all roles to Application resources (takes precedence over merged XAML dictionaries)
            var res = System.Windows.Application.Current.Resources;
            res["AppBackground"]          = new SolidColorBrush(surface);
            res["AppBorder"]              = new SolidColorBrush(borderColor);
            res["PrimaryTextForeground"]  = new SolidColorBrush(onSurface);
            res["SecondaryTextForeground"]= new SolidColorBrush(secondaryFg);
            res["ButtonBackground"]       = new SolidColorBrush(surfaceVar);
            res["ButtonIconForeground"]   = new SolidColorBrush(onSurface);
            res["SliderTrackBackground"]  = new SolidColorBrush(sliderTrack);
            res["SliderTrackHover"]       = new SolidColorBrush(sliderHover);
            res["SliderThumb"]            = new SolidColorBrush(onSurface);
            res["AlbumArtBackground"]     = new SolidColorBrush(surfaceVar);
            res["AlbumArtBorder"]         = new SolidColorBrush(albumArtBorder);
            res["TooltipBackground"]      = new SolidColorBrush(tooltipBg);
            res["TooltipForeground"]      = new SolidColorBrush(onSurface);
            res["WindowShadowOpacity"]    = isDark ? 0.65 : 0.35;

            // Ensure title uses the theme foreground (not a separately coloured brush)
            TitleText.SetResourceReference(
                System.Windows.Controls.TextBlock.ForegroundProperty,
                "PrimaryTextForeground");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Colour helpers
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the dominant (average) colour of a BitmapSource by sampling
        /// every 5th pixel in a 32×32 thumbnail for performance, then boosts saturation.
        /// </summary>
        private static System.Windows.Media.Color GetDominantColor(BitmapSource source)
        {
            var scaled = new TransformedBitmap(source,
                new ScaleTransform(32.0 / source.PixelWidth, 32.0 / source.PixelHeight));

            int width         = scaled.PixelWidth;
            int height        = scaled.PixelHeight;
            int bytesPerPixel = (scaled.Format.BitsPerPixel + 7) / 8;
            int stride        = width * bytesPerPixel;
            byte[] pixels     = new byte[height * stride];
            scaled.CopyPixels(pixels, stride, 0);

            long r = 0, g = 0, b = 0, count = 0;
            for (int i = 0; i < pixels.Length; i += bytesPerPixel * 5)
            {
                if (i + 2 >= pixels.Length) break;
                b += pixels[i];
                g += pixels[i + 1];
                r += pixels[i + 2];
                count++;
            }

            if (count == 0) return Colors.White;

            byte avgR = (byte)(r / count);
            byte avgG = (byte)(g / count);
            byte avgB = (byte)(b / count);

            // Boost saturation — blend away from grey
            float grey  = 0.299f * avgR + 0.587f * avgG + 0.114f * avgB;
            float boost = 1.8f;
            byte vR = Clamp((int)(grey + boost * (avgR - grey)), 0, 255);
            byte vG = Clamp((int)(grey + boost * (avgG - grey)), 0, 255);
            byte vB = Clamp((int)(grey + boost * (avgB - grey)), 0, 255);

            // Guarantee minimum brightness for readability on dark widget
            float brightness = 0.299f * vR + 0.587f * vG + 0.114f * vB;
            if (brightness < 80)
            {
                float scale = 80f / Math.Max(brightness, 1);
                vR = Clamp((int)(vR * scale), 0, 255);
                vG = Clamp((int)(vG * scale), 0, 255);
                vB = Clamp((int)(vB * scale), 0, 255);
            }

            return System.Windows.Media.Color.FromRgb(vR, vG, vB);
        }

        private static byte Clamp(int value, int min, int max)
            => (byte)Math.Max(min, Math.Min(max, value));
    }
}