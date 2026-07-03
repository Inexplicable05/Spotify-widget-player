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

namespace CapsulePlayerWpf
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
        private DispatcherTimer _processCheckTimer;
        private SettingsWindow? _settingsWindow;

        private bool _isSpotifyRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            TrayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

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
            catch (Exception ex)
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
                AlbumArtImage.Source = null;
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
                    AlbumArtImage.Source = bitmap;
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

                BtnPrev.IsEnabled = playbackInfo.Controls.IsPreviousEnabled;
                BtnPlayPause.IsEnabled = playbackInfo.Controls.IsPlayPauseToggleEnabled || playbackInfo.Controls.IsPlayEnabled || playbackInfo.Controls.IsPauseEnabled;
                BtnNext.IsEnabled = playbackInfo.Controls.IsNextEnabled;
            }
            
            var timeline = _currentSession.GetTimelineProperties();
            if (timeline != null && !_isDraggingSlider)
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
    }
}