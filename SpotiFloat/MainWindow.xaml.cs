using System;
using System.IO;
using System.Linq;
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
using SpotiFloat.Controls;
using WpfApp     = System.Windows.Application;
using WpfColor   = System.Windows.Media.Color;

namespace SpotiFloat
{
    public enum WidgetZOrder { Topmost, Desktop }

    public partial class MainWindow : Window
    {
        // â”€â”€ P/Invoke â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_BOTTOM  = new IntPtr(1);
        const uint SWP_NOSIZE     = 0x0001;
        const uint SWP_NOMOVE     = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;

        // â”€â”€ Public widget state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public WidgetZOrder ZOrder         { get; set; } = WidgetZOrder.Topmost;
        public bool         HideOnFullscreen { get; set; } = false;
        public bool         IsLocked        { get; set; } = false;
        public int          ThemeIndex      { get; set; } = 0;
        public AlbumColourMode AlbumColouring { get; set; } = AlbumColourMode.Off;
        private BitmapSource? _paletteArtwork;
        private WpfColor? _albumAccent;
        public PlayerLayout Layout          { get; set; } = PlayerLayout.Bar;

        // Expose scale transform of the active player for Settings window
        public ScaleTransform? AppScaleTransform =>
            (_activeControl as IPlayerControl)?.AppScaleTransform;

        // Album art of the currently active player.
        public BitmapSource? CurrentAlbumArt => _lastState?.Art;

        // â”€â”€ Media session â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private GlobalSystemMediaTransportControlsSession?        _currentSession;
        private DispatcherTimer _timer;
        private DispatcherTimer _sessionCheckTimer;
        private string _lastTrackTitle = string.Empty;
        private DateTime _lastSeekTime = DateTime.MinValue;
        private MediaState? _lastState;

        // â”€â”€ Layout â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private FrameworkElement? _activeControl;
        private IPlayerControl? _activePlayer => _activeControl as IPlayerControl;
        private SettingsWindow? _settingsWindow;

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Constructor
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public MainWindow()
        {
            InitializeComponent();

            UpdateTrayIcon();
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
            Closed += (_, _) =>
            {
                Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
                TrayIcon.Dispose();
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;

            _sessionCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _sessionCheckTimer.Tick += SessionCheckTimer_Tick;
            _sessionCheckTimer.Start();

            Loaded += MainWindow_Loaded;
        }

        private void OnSystemPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
            => Dispatcher.BeginInvoke(new Action(UpdateTrayIcon));

        private void UpdateTrayIcon()
        {
            bool light = false;
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                light = key?.GetValue("SystemUsesLightTheme") is int value && value == 1;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                Debug.WriteLine($"Tray theme lookup failed: {ex}");
            }
            TrayIcon.IconSource = BitmapFrame.Create(new Uri($"pack://application:,,,/SpotiFloat;component/tray-{(light ? "light" : "dark")}.ico"));
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Layout switching
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void SwitchLayout(PlayerLayout layout)
        {
            Layout = layout;

            // Unsubscribe old control events
            if (_activePlayer != null)
            {
                _activePlayer.SeekRequested       -= OnSeekRequested;
                _activePlayer.PrevRequested        -= OnPrevRequested;
                _activePlayer.PlayPauseRequested   -= OnPlayPauseRequested;
                _activePlayer.NextRequested        -= OnNextRequested;
            }

            // Create new control
            IPlayerControl newControl = layout switch
            {
                PlayerLayout.Square => new SquarePlayerControl(),
                PlayerLayout.Disk   => new DiskPlayerControl(),
                _                   => new BarPlayerControl()
            };

            // Subscribe events
            newControl.SeekRequested       += OnSeekRequested;
            newControl.PrevRequested        += OnPrevRequested;
            newControl.PlayPauseRequested   += OnPlayPauseRequested;
            newControl.NextRequested        += OnNextRequested;

            _activeControl      = (FrameworkElement)newControl;
            PlayerHost.Content  = _activeControl;

            // Apply current scale
            var settings = SettingsManager.LoadSettings();
            if (newControl.AppScaleTransform != null)
            {
                newControl.AppScaleTransform.ScaleX = settings.Scale;
                newControl.AppScaleTransform.ScaleY = settings.Scale;
            }

            // Push the last known media state immediately
            if (_lastState != null)
                newControl.UpdateMediaState(_lastState);
            RefreshAppearance();

        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Media command callbacks from active control
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private async void OnSeekRequested(double seconds)
        {
            if (_currentSession == null) return;
            _lastSeekTime = DateTime.Now;
            if (_lastState != null) _lastState.Position = seconds;
            await _currentSession.TryChangePlaybackPositionAsync(
                (long)(seconds * 10_000_000));
        }

        private async void OnPrevRequested()
        {
            if (_currentSession != null) await _currentSession.TrySkipPreviousAsync();
        }

        private async void OnPlayPauseRequested()
        {
            if (_currentSession != null) await _currentSession.TryTogglePlayPauseAsync();
        }

        private async void OnNextRequested()
        {
            if (_currentSession != null) await _currentSession.TrySkipNextAsync();
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Visibility / Z-Order
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void SessionCheckTimer_Tick(object? sender, EventArgs e)
        {
            UpdateCurrentSession();
        }

        private bool IsFullscreenAppActive()
        {
            IntPtr fgWnd = GetForegroundWindow();
            if (fgWnd == IntPtr.Zero) return false;
            var cls = new System.Text.StringBuilder(256);
            GetClassName(fgWnd, cls, cls.Capacity);
            if (cls.ToString() == "WorkerW" || cls.ToString() == "Progman") return false;
            if (GetWindowRect(fgWnd, out RECT rect))
            {
                var screen = System.Windows.Forms.Screen.FromHandle(fgWnd);
                if (rect.Right - rect.Left >= screen.Bounds.Width &&
                    rect.Bottom - rect.Top >= screen.Bounds.Height)
                    return true;
            }
            return false;
        }

        private void UpdateVisibility()
        {
            bool show = _currentSession != null;
            if (show && ZOrder == WidgetZOrder.Topmost && HideOnFullscreen && IsFullscreenAppActive())
                show = false;

            if (show && Visibility != Visibility.Visible)
                Visibility = Visibility.Visible;
            else if (!show && Visibility == Visibility.Visible)
            {
                Visibility = Visibility.Hidden;
                if (_currentSession == null) _settingsWindow?.Close();
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Startup & media session
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load & apply saved settings
            var s = SettingsManager.LoadSettings();
            IsLocked                = s.IsLocked;
            ZOrder                  = s.ZOrder;
            HideOnFullscreen        = s.HideOnFullscreen;
            ThemeIndex              = s.ThemeIndex;
            AlbumColouring          = s.AlbumColouring;

            ApplyTheme(s.ThemeIndex);

            // Activate saved layout.
            SwitchLayout(s.Layout);

            // Media session
            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_manager != null)
                {
                    _manager.CurrentSessionChanged += (_, _) =>
                        Dispatcher.Invoke(UpdateCurrentSession);
                    _manager.SessionsChanged += (_, _) =>
                        Dispatcher.Invoke(UpdateCurrentSession);
                    UpdateCurrentSession();
                }
            }
            catch { }
        }

        private void UpdateCurrentSession()
        {
            if (_manager == null) return;
            GlobalSystemMediaTransportControlsSession? newSession;
            string? audioAppId = null;
            bool audioPlaying = false;
            try
            {
                var sessions = _manager.GetSessions()
                    .Where(s => SupportedMusicSources.IsAllowed(s.SourceAppUserModelId)).ToList();
                var preferred = _manager.GetCurrentSession();
                bool IsPlaying(GlobalSystemMediaTransportControlsSession s) =>
                    s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                newSession = sessions.FirstOrDefault(s => s == preferred && IsPlaying(s))
                    ?? sessions.FirstOrDefault(s => s == _currentSession && IsPlaying(s))
                    ?? sessions.FirstOrDefault(IsPlaying)
                    ?? sessions.FirstOrDefault(s => s == _currentSession)
                    ?? sessions.FirstOrDefault();
                audioAppId = newSession?.SourceAppUserModelId;
                audioPlaying = newSession != null && IsPlaying(newSession);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Media session selection failed: {ex}");
                newSession = null;
            }
            if (_currentSession != newSession)
            {
                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                }
                _currentSession = newSession;
                _lastTrackTitle = string.Empty;
                _lastState = null;
                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged += Session_PlaybackInfoChanged;
                }
            }
            UpdateMediaState();
            AudioProcessor.Instance.SetSource(audioAppId, audioPlaying);
            UpdateVisibility();
            if (_currentSession != null) _timer.Start(); else _timer.Stop();
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
            => Dispatcher.Invoke(UpdateMediaState);

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
            => Dispatcher.Invoke(UpdateCurrentSession);

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateVisibility();
            EnforceZOrder();
            UpdateMediaState();
        }

        private void EnforceZOrder()
        {
            if (Visibility != Visibility.Visible) return;
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            if (ZOrder == WidgetZOrder.Topmost)
            {
                if (!Topmost) Topmost = true;
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            else
            {
                if (Topmost) Topmost = false;
                SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

        private async void UpdateMediaState()
        {
            if (_activePlayer == null) return;

            if (_currentSession == null)
            {
                var empty = new MediaState
                {
                    Title     = "No Media Available",
                    Artist    = "No media data available yet",
                    IsPlaying = false
                };
                _lastState = empty;
                _activePlayer.UpdateMediaState(empty);
                RefreshAppearance();
                return;
            }

            try
            {
                var session = _currentSession;
                var props      = await session.TryGetMediaPropertiesAsync();
                if (session != _currentSession) return;
                var playback   = session.GetPlaybackInfo();
                var timeline   = session.GetTimelineProperties();

                bool isPlaying = playback?.PlaybackStatus ==
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                double pos = 0, dur = 0;
                if (timeline != null)
                {
                    pos = timeline.Position.TotalSeconds;
                    dur = timeline.EndTime.TotalSeconds > 0 ? timeline.EndTime.TotalSeconds : 100;
                }
                else if (_lastState != null)
                {
                    pos = _lastState.Position;
                    dur = _lastState.Duration;
                }

                // Album art
                BitmapSource? art = null;
                if (props?.Thumbnail != null)
                {
                    using var stream = await props.Thumbnail.OpenReadAsync();
                    var buf  = new Windows.Storage.Streams.Buffer((uint)stream.Size);
                    await stream.ReadAsync(buf, buf.Capacity, InputStreamOptions.None);
                    using var dr = DataReader.FromBuffer(buf);
                    byte[] bytes = new byte[buf.Length];
                    dr.ReadBytes(bytes);
                    using var ms = new MemoryStream(bytes);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption  = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    art = bmp;
                }

                if (session != _currentSession) return;
                string title = props?.Title ?? "Unknown Title";
                bool   isNew = title != _lastTrackTitle;
                _lastTrackTitle = title;

                var state = new MediaState
                {
                    Title     = title,
                    Artist    = props?.Artist ?? "Unknown Artist",
                    Art       = art,
                    IsPlaying = isPlaying,
                    Position  = pos,
                    Duration  = dur,
                    IsNewTrack = isNew
                };
                _lastState = state;
                _activePlayer.UpdateMediaState(state);

                RefreshAppearance();
            }
            catch { }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Window events
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !IsLocked) DragMove();
        }

        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
            => OpenSettings();

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
            => OpenSettings();

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            WpfApp.Current.Shutdown();
        }

        public void OpenSettings()
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow(this);
                _settingsWindow.Show();
            }
            else _settingsWindow.Activate();
        }

        public void RefreshMedia() => UpdateCurrentSession();

        public void ApplyTheme(int themeIndex)
        {
            ThemeIndex = Math.Clamp(themeIndex, 0, 2);
            string themeFile = ThemeIndex switch
            {
                1 => "Theme_Light.xaml",
                2 => "Theme_Glass.xaml",
                _ => "Theme_Dark.xaml"
            };
            var resources = WpfApp.Current.Resources.MergedDictionaries;
            resources.Clear();
            resources.Add(new ResourceDictionary { Source = new Uri($"/SpotiFloat;component/Themes/{themeFile}", UriKind.Relative) });
            RefreshAppearance();
        }

        public void RefreshAppearance()
        {
            if (_activeControl == null) return;
            var artwork = _lastState?.Art;
            if (!ReferenceEquals(artwork, _paletteArtwork))
            {
                _paletteArtwork = artwork;
                _albumAccent = artwork == null ? null : AlbumPalette.Extract(artwork);
            }
            AlbumPalette.Apply(_activeControl, ThemeIndex, AlbumColouring, _albumAccent);
            _activePlayer?.ApplyTitleAppearance(ThemeIndex,
                AlbumColouring == AlbumColourMode.Off ? null : _albumAccent);
        }
    }
}
