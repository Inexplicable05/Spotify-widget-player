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
using WpfColors  = System.Windows.Media.Colors;

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
        public bool         EnableDynamicTitleColour { get; set; } = true;
        public PlayerLayout Layout          { get; set; } = PlayerLayout.Bar;

        // Expose scale transform of the active player for Settings window
        public ScaleTransform? AppScaleTransform =>
            (_activeControl as IPlayerControl)?.AppScaleTransform;

        // Album art of the currently active player (for dynamic theme)
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
        private UIElement?   _activeControl;
        private IPlayerControl? _activePlayer => _activeControl as IPlayerControl;
        private SettingsWindow? _settingsWindow;

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Constructor
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public MainWindow()
        {
            InitializeComponent();

            string? exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
                TrayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;

            _sessionCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _sessionCheckTimer.Tick += SessionCheckTimer_Tick;
            _sessionCheckTimer.Start();

            Loaded += MainWindow_Loaded;
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

            _activeControl      = (UIElement)newControl;
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
            RefreshTitleColour();

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
            EnableDynamicTitleColour= s.EnableDynamicTitleColour;

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
            ThemeIndex = themeIndex;
            ClearDynamicThemeOverrides();
            string themeFile = themeIndex switch
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
            if (ThemeIndex == 3)
            {
                if (_lastState?.Art != null) ApplyDynamicTheme(_lastState.Art);
                else ClearDynamicThemeOverrides();
            }
            RefreshTitleColour();
        }

        private void RefreshTitleColour()
        {
            WpfColor? accent = EnableDynamicTitleColour && _lastState?.Art != null
                ? GetDominantColor(_lastState.Art) : null;
            _activePlayer?.ApplyTitleAppearance(ThemeIndex, accent);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Dynamic Theme
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static readonly string[] DynamicThemeKeys =
        {
            "AppBackground", "AppBorder", "PrimaryTextForeground", "SecondaryTextForeground",
            "ButtonBackground", "ButtonIconForeground",
            "SliderTrackBackground", "SliderTrackHover", "SliderThumb",
            "AlbumArtBackground", "AlbumArtBorder",
            "TooltipBackground", "TooltipForeground",
            "WindowShadowOpacity",
            "SettingsPopupBackground", "SettingsPopupBorder",
            "SettingsElementBackground", "SettingsElementBorder",
            "SettingsElementHover", "SettingsIconForeground", "PlayerOverlayColour"
        };

        public static void ClearDynamicThemeOverrides()
        {
            var res = WpfApp.Current.Resources;
            foreach (var key in DynamicThemeKeys)
                if (res.Contains(key)) res.Remove(key);
        }

        public void ApplyDynamicTheme(BitmapSource source)
        {
            WpfColor seed = GetDominantColor(source);
            byte sR = (byte)(seed.R * 0.72 + 18);
            byte sG = (byte)(seed.G * 0.72 + 18);
            byte sB = (byte)(seed.B * 0.72 + 18);
            WpfColor surface = WpfColor.FromRgb(sR, sG, sB);

            WpfColor onSurface = TextContrast.ReadableOn(surface);
            bool isDark = onSurface == WpfColors.White;
            WpfColor secondaryFg = onSurface;
            WpfColor surfaceVar   = isDark
                ? WpfColor.FromRgb(Clamp(sR - 15, 0, 255), Clamp(sG - 15, 0, 255), Clamp(sB - 15, 0, 255))
                : WpfColor.FromRgb(Clamp(sR + 15, 0, 255), Clamp(sG + 15, 0, 255), Clamp(sB + 15, 0, 255));
            WpfColor borderColor  = WpfColor.FromArgb(isDark ? (byte)70  : (byte)50,  0, 0, 0);
            WpfColor sliderTrack  = WpfColor.FromArgb(isDark ? (byte)55  : (byte)45,  onSurface.R, onSurface.G, onSurface.B);
            WpfColor sliderHover  = WpfColor.FromArgb(isDark ? (byte)110 : (byte)90,  onSurface.R, onSurface.G, onSurface.B);
            WpfColor albumBorder  = WpfColor.FromArgb(isDark ? (byte)80  : (byte)60,  onSurface.R, onSurface.G, onSurface.B);
            WpfColor tooltipBg    = WpfColor.FromRgb(
                Clamp(isDark ? sR - 20 : sR + 20, 0, 255),
                Clamp(isDark ? sG - 20 : sG + 20, 0, 255),
                Clamp(isDark ? sB - 20 : sB + 20, 0, 255));

            var res = WpfApp.Current.Resources;
            res["AppBackground"]          = new SolidColorBrush(surface);
            res["AppBorder"]              = new SolidColorBrush(borderColor);
            res["PrimaryTextForeground"]  = new SolidColorBrush(onSurface);
            res["PlayerOverlayColour"] = surface;
            res["SecondaryTextForeground"]= new SolidColorBrush(secondaryFg);
            res["ButtonBackground"]       = new SolidColorBrush(surfaceVar);
            res["ButtonIconForeground"]   = new SolidColorBrush(onSurface);
            res["SliderTrackBackground"]  = new SolidColorBrush(sliderTrack);
            res["SliderTrackHover"]       = new SolidColorBrush(sliderHover);
            res["SliderThumb"]            = new SolidColorBrush(onSurface);
            res["AlbumArtBackground"]     = new SolidColorBrush(surfaceVar);
            res["AlbumArtBorder"]         = new SolidColorBrush(albumBorder);
            res["TooltipBackground"]      = new SolidColorBrush(tooltipBg);
            res["TooltipForeground"]      = new SolidColorBrush(onSurface);
            res["WindowShadowOpacity"]    = isDark ? 0.65 : 0.35;

            if (ThemeIndex == 3)
            {
                WpfColor settingsBg = surface;
                byte overlay = isDark ? (byte)0 : (byte)255;
                WpfColor settingsEl = WpfColor.FromArgb(45, overlay, overlay, overlay);
                WpfColor settingsHov = WpfColor.FromArgb(70, overlay, overlay, overlay);
                res["SettingsPopupBackground"]  = new SolidColorBrush(settingsBg);
                res["SettingsPopupBorder"]      = new SolidColorBrush(borderColor);
                res["SettingsElementBackground"]= new SolidColorBrush(settingsEl);
                res["SettingsElementBorder"]    = new SolidColorBrush(settingsEl);
                res["SettingsElementHover"]     = new SolidColorBrush(settingsHov);
                res["SettingsIconForeground"]   = new SolidColorBrush(secondaryFg);
            }
            RefreshTitleColour();
        }

        private static WpfColor GetDominantColor(BitmapSource source)
        {
            var scaled = new TransformedBitmap(source,
                new ScaleTransform(32.0 / source.PixelWidth, 32.0 / source.PixelHeight));
            int bpp    = (scaled.Format.BitsPerPixel + 7) / 8;
            int stride = scaled.PixelWidth * bpp;
            byte[] px  = new byte[scaled.PixelHeight * stride];
            scaled.CopyPixels(px, stride, 0);

            long r = 0, g = 0, b = 0, n = 0;
            for (int i = 0; i < px.Length; i += bpp * 5)
            {
                if (i + 2 >= px.Length) break;
                b += px[i]; g += px[i + 1]; r += px[i + 2]; n++;
            }
            if (n == 0) return WpfColors.White;

            byte avgR = (byte)(r / n), avgG = (byte)(g / n), avgB = (byte)(b / n);
            float grey  = 0.299f * avgR + 0.587f * avgG + 0.114f * avgB;
            float boost = 1.8f;
            byte  vR    = Clamp((int)(grey + boost * (avgR - grey)), 0, 255);
            byte  vG    = Clamp((int)(grey + boost * (avgG - grey)), 0, 255);
            byte  vB    = Clamp((int)(grey + boost * (avgB - grey)), 0, 255);

            float brightness = 0.299f * vR + 0.587f * vG + 0.114f * vB;
            if (brightness < 80)
            {
                float scale = 80f / Math.Max(brightness, 1);
                vR = Clamp((int)(vR * scale), 0, 255);
                vG = Clamp((int)(vG * scale), 0, 255);
                vB = Clamp((int)(vB * scale), 0, 255);
            }
            return WpfColor.FromRgb(vR, vG, vB);
        }

        private static byte Clamp(int v, int lo, int hi)
            => (byte)Math.Max(lo, Math.Min(hi, v));
    }
}
