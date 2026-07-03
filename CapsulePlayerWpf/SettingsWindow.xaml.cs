using System;
using System.Windows;

namespace CapsulePlayerWpf
{
    public partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            LockCheckBox.IsChecked = _mainWindow.IsLocked;
            ZOrderComboBox.SelectedIndex = _mainWindow.ZOrder == WidgetZOrder.Topmost ? 0 : 1;
            HideOnFullscreenCheckBox.IsChecked = _mainWindow.HideOnFullscreen;
            HideOnFullscreenCheckBox.IsEnabled = _mainWindow.ZOrder == WidgetZOrder.Topmost;
            ScaleSlider.Value = _mainWindow.AppScaleTransform.ScaleX;
        }

        private void LockCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _mainWindow.IsLocked = LockCheckBox.IsChecked == true;
        }

        private void ZOrderComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_mainWindow != null)
            {
                _mainWindow.ZOrder = ZOrderComboBox.SelectedIndex == 0 ? WidgetZOrder.Topmost : WidgetZOrder.Desktop;
                if (HideOnFullscreenCheckBox != null)
                {
                    HideOnFullscreenCheckBox.IsEnabled = _mainWindow.ZOrder == WidgetZOrder.Topmost;
                }
            }
        }

        private void HideOnFullscreenCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
            {
                _mainWindow.HideOnFullscreen = HideOnFullscreenCheckBox.IsChecked == true;
            }
        }

        private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mainWindow != null && _mainWindow.AppScaleTransform != null)
            {
                _mainWindow.AppScaleTransform.ScaleX = e.NewValue;
                _mainWindow.AppScaleTransform.ScaleY = e.NewValue;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.RefreshMedia();
            System.Windows.MessageBox.Show(this, "Media connection refreshed.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            LockCheckBox.IsChecked = false;
            ZOrderComboBox.SelectedIndex = 0;
            HideOnFullscreenCheckBox.IsChecked = false;
            ScaleSlider.Value = 1.0;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
