using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace CapsulePlayerWpf
{
    public partial class MainWindow : Window
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        private DispatcherTimer _timer;
        private bool _isDraggingSlider = false;

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;
            Loaded += MainWindow_Loaded;
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
                StatusText.Text = $"Failed to connect to media manager: {ex.Message}";
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
            UpdateMediaState();
        }

        private async void UpdateMediaState()
        {
            if (_currentSession == null)
            {
                TitleText.Text = "No Media Available";
                ArtistText.Text = "";
                StatusText.Text = "Waiting for media...";
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
                StatusText.Text = isPlaying ? "Playing" : "Paused";
                
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
            if (e.ChangedButton == MouseButton.Left)
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
    }
}