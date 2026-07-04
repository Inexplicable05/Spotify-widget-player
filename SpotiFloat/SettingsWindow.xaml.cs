using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SpotiFloat
{
    public partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;
        private bool _isInitializing = false;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            var settings = SettingsManager.LoadSettings();
            
            _isInitializing = true;
            UpdateThemeButtons(settings.ThemeIndex);
            LockCheckBox.IsChecked = settings.IsLocked;
            ZOrderComboBox.SelectedIndex = settings.ZOrder == WidgetZOrder.Topmost ? 0 : 1;
            HideOnFullscreenCheckBox.IsChecked = settings.HideOnFullscreen;
            HideFullscreenContainer.Visibility = settings.ZOrder == WidgetZOrder.Topmost ? Visibility.Visible : Visibility.Collapsed;
            ScaleTextBox.Text = (settings.Scale * 100).ToString("0");
            DynamicTitleColourCheckBox.IsChecked = settings.EnableDynamicTitleColour;
            _isInitializing = false;

            // Apply initial lock state for Dynamic Title Colour toggle (Option A)
            UpdateDynamicTitleColourState(settings.ThemeIndex);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ── Theme button helpers ──────────────────────────────────────────────

        private void UpdateThemeButtons(int themeIndex)
        {
            ThemeDarkBtn.Background    = new SolidColorBrush(Colors.Transparent);
            ThemeLightBtn.Background   = new SolidColorBrush(Colors.Transparent);
            ThemeGlassBtn.Background   = new SolidColorBrush(Colors.Transparent);
            ThemeDynamicBtn.Background = new SolidColorBrush(Colors.Transparent);

            // Semi-transparent white highlight for the active button
            var activeBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
            if      (themeIndex == 0) ThemeDarkBtn.Background    = activeBrush;
            else if (themeIndex == 1) ThemeLightBtn.Background   = activeBrush;
            else if (themeIndex == 2) ThemeGlassBtn.Background   = activeBrush;
            else if (themeIndex == 3) ThemeDynamicBtn.Background = activeBrush;
        }

        /// <summary>Returns the currently selected theme index based on highlighted button.</summary>
        private int GetCurrentThemeIndex()
        {
            if (ThemeLightBtn.Background   is SolidColorBrush lb && lb.Color.A != 0) return 1;
            if (ThemeGlassBtn.Background   is SolidColorBrush gb && gb.Color.A != 0) return 2;
            if (ThemeDynamicBtn.Background is SolidColorBrush db && db.Color.A != 0) return 3;
            return 0;
        }

        private void ThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn &&
                  int.TryParse(btn.Tag?.ToString(), out int themeIndex))) return;

            UpdateThemeButtons(themeIndex);

            if (themeIndex == 3)
            {
                // Dynamic theme: apply palette from current album art if available,
                // otherwise load Dark as a safe base and wait for the next track.
                var art = _mainWindow.CurrentAlbumArt;
                if (art != null)
                {
                    _mainWindow.ApplyDynamicTheme(art);
                }
                else
                {
                    // No art yet — load Dark theme as fallback; palette applies on first track
                    MainWindow.ClearDynamicThemeOverrides();
                    var darkDict = new ResourceDictionary { Source = new Uri("Themes/Theme_Dark.xaml", UriKind.Relative) };
                    System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
                    System.Windows.Application.Current.Resources.MergedDictionaries.Add(darkDict);
                }
            }
            else
            {
                // Static theme: remove any dynamic overrides, then load the XAML file
                MainWindow.ClearDynamicThemeOverrides();
                string themeFile = themeIndex switch
                {
                    1 => "Themes/Theme_Light.xaml",
                    2 => "Themes/Theme_Glass.xaml",
                    _ => "Themes/Theme_Dark.xaml"
                };
                var dict = new ResourceDictionary { Source = new Uri(themeFile, UriKind.Relative) };
                System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
            }

            // Option A: lock / unlock the Dynamic Title Colour toggle
            UpdateDynamicTitleColourState(themeIndex);

            SettingChanged(null!, null!);
        }

        /// <summary>
        /// Option A — When Dynamic theme is active, grey out and label the
        /// Dynamic Title Colour toggle so the user knows it is managed automatically.
        /// </summary>
        private void UpdateDynamicTitleColourState(int themeIndex)
        {
            bool isDynamic = (themeIndex == 3);

            DynamicTitleColourCheckBox.IsEnabled = !isDynamic;
            DynamicTitleColourCheckBox.Opacity   = isDynamic ? 0.4 : 1.0;
            DynamicTitleManagedLabel.Visibility  = isDynamic ? Visibility.Visible   : Visibility.Collapsed;
            DynamicTitleNormalLabel.Visibility   = isDynamic ? Visibility.Collapsed : Visibility.Visible;
        }

        // ── Danger Zone ───────────────────────────────────────────────────────

        private void DangerZoneBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DangerZoneContent.Visibility == Visibility.Collapsed)
            {
                DangerZoneContent.Visibility = Visibility.Visible;
                DangerZoneIcon.Data = Geometry.Parse("M19 15l-7-7-7 7");
            }
            else
            {
                DangerZoneContent.Visibility = Visibility.Collapsed;
                DangerZoneIcon.Data = Geometry.Parse("M19 9l-7 7-7-7");
            }
        }

        // ── Settings persistence ──────────────────────────────────────────────

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _mainWindow == null) return;
            
            _mainWindow.IsLocked = LockCheckBox.IsChecked == true;
            
            _mainWindow.ZOrder = ZOrderComboBox.SelectedIndex == 0 ? WidgetZOrder.Topmost : WidgetZOrder.Desktop;
            HideFullscreenContainer.Visibility = _mainWindow.ZOrder == WidgetZOrder.Topmost ? Visibility.Visible : Visibility.Collapsed;
            _mainWindow.HideOnFullscreen = HideOnFullscreenCheckBox.IsChecked == true;

            // Dynamic Title Colour only matters when toggle is enabled (not locked by Dynamic theme)
            _mainWindow.EnableDynamicTitleColour = DynamicTitleColourCheckBox.IsChecked == true;
            
            if (_mainWindow.AppScaleTransform != null)
            {
                double scale = GetScaleValue();
                _mainWindow.AppScaleTransform.ScaleX = scale;
                _mainWindow.AppScaleTransform.ScaleY = scale;
            }

            int themeIndex = GetCurrentThemeIndex();
            _mainWindow.ThemeIndex = themeIndex;

            var settings = new AppSettings
            {
                ThemeIndex               = themeIndex,
                IsLocked                 = _mainWindow.IsLocked,
                ZOrder                   = _mainWindow.ZOrder,
                HideOnFullscreen         = _mainWindow.HideOnFullscreen,
                Scale                    = GetScaleValue(),
                EnableDynamicTitleColour = _mainWindow.EnableDynamicTitleColour
            };
            
            SettingsManager.SaveSettings(settings);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.RefreshMedia();
            System.Windows.MessageBox.Show(this, "Media connection refreshed.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            UpdateThemeButtons(0);
            LockCheckBox.IsChecked = false;
            ZOrderComboBox.SelectedIndex = 0;
            HideOnFullscreenCheckBox.IsChecked = false;
            ScaleTextBox.Text = "100";
            DynamicTitleColourCheckBox.IsChecked = true;
            _isInitializing = false;

            // Restore Dynamic Title Colour toggle to unlocked state (theme 0 = Dark)
            UpdateDynamicTitleColourState(0);

            // Apply Dark theme and save
            ThemeBtn_Click(ThemeDarkBtn, null!);
        }

        private void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "This will wipe all preferences, remove the startup registry key, and immediately shut down the app. Are you sure you want to uninstall this portable app's data?",
                "Uninstall & Clean", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                SettingsManager.WipeSettingsAndUninstall();
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private double GetScaleValue()
        {
            if (double.TryParse(ScaleTextBox.Text, out double percent))
            {
                double scale = percent / 100.0;
                if (scale < 0.1) scale = 0.1;
                if (scale > 3.0) scale = 3.0;
                return scale;
            }
            return 1.0;
        }

        private void ScaleTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "[0-9]");
        }

        private void ScaleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SettingChanged(null!, null!);
        }
    }
}
