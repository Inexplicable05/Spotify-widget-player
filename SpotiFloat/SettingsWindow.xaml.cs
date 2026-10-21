using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RadioButton = System.Windows.Controls.RadioButton;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SpotiFloat;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;
    private bool _isInitializing = true;

    public SettingsWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        InitializeComponent();
        MaxHeight = Math.Min(MaxHeight, SystemParameters.WorkArea.Height);
        LoadControls(SettingsManager.LoadSettings());
    }

    private void LoadControls(AppSettings settings)
    {
        _isInitializing = true;
        LayoutBarBtn.IsChecked = settings.Layout == PlayerLayout.Bar;
        LayoutSquareBtn.IsChecked = settings.Layout == PlayerLayout.Square;
        LayoutDiskBtn.IsChecked = settings.Layout == PlayerLayout.Disk;
        ThemeDarkBtn.IsChecked = settings.ThemeIndex == 0;
        ThemeLightBtn.IsChecked = settings.ThemeIndex == 1;
        ThemeGlassBtn.IsChecked = settings.ThemeIndex == 2;
        ColourOffBtn.IsChecked = settings.AlbumColouring == AlbumColourMode.Off;
        ColourTitleBtn.IsChecked = settings.AlbumColouring == AlbumColourMode.Title;
        ColourAccentsBtn.IsChecked = settings.AlbumColouring == AlbumColourMode.Accents;
        ColourFullBtn.IsChecked = settings.AlbumColouring == AlbumColourMode.Full;
        LockCheckBox.IsChecked = settings.IsLocked;
        ZOrderTopmostChoice.IsChecked = settings.ZOrder == WidgetZOrder.Topmost;
        ZOrderDesktopChoice.IsChecked = settings.ZOrder == WidgetZOrder.Desktop;
        HideOnFullscreenCheckBox.IsChecked = settings.HideOnFullscreen;
        RunOnStartupCheckBox.IsChecked = settings.RunOnStartup;
        ScaleSlider.Value = double.IsFinite(settings.Scale) ? Math.Clamp(settings.Scale * 100, 10, 300) : 100;
        ScaleTextBox.Text = ScaleSlider.Value.ToString("0");
        UpdateDependentControls();
        _isInitializing = false;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void LayoutChoice_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        if (sender is RadioButton button && int.TryParse(button.Tag?.ToString(), out int layout))
        {
            _mainWindow.SwitchLayout((PlayerLayout)layout);
            SettingChanged(sender, e);
        }
    }

    private void ThemeChoice_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        if (sender is RadioButton button && int.TryParse(button.Tag?.ToString(), out int theme))
        {
            _mainWindow.ApplyTheme(theme);
            SettingChanged(sender, e);
        }
    }

    private void UpdateDependentControls()
    {
        AlbumColourHint.Text = ColourFullBtn.IsChecked == true ? "Album accents and a tinted player backdrop. Glass stays translucent."
            : ColourAccentsBtn.IsChecked == true ? "Album colour on the title, controls, progress and visualizer."
            : ColourTitleBtn.IsChecked == true ? "Album colour on the song title only."
            : "Neutral theme colours, independent of album artwork.";
        HideOnFullscreenCheckBox.IsEnabled = ZOrderTopmostChoice.IsChecked == true;
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _mainWindow.IsLocked = LockCheckBox.IsChecked == true;
        _mainWindow.ZOrder = ZOrderTopmostChoice.IsChecked == true ? WidgetZOrder.Topmost : WidgetZOrder.Desktop;
        _mainWindow.HideOnFullscreen = HideOnFullscreenCheckBox.IsChecked == true;
        _mainWindow.AlbumColouring = ColourFullBtn.IsChecked == true ? AlbumColourMode.Full
            : ColourAccentsBtn.IsChecked == true ? AlbumColourMode.Accents
            : ColourTitleBtn.IsChecked == true ? AlbumColourMode.Title : AlbumColourMode.Off;
        if (_mainWindow.AppScaleTransform is { } scale)
            scale.ScaleX = scale.ScaleY = ScaleSlider.Value / 100;
        UpdateDependentControls();
        _mainWindow.RefreshAppearance();
        SettingsManager.SaveSettings(new AppSettings
        {
            Layout = _mainWindow.Layout,
            ThemeIndex = _mainWindow.ThemeIndex,
            IsLocked = _mainWindow.IsLocked,
            ZOrder = _mainWindow.ZOrder,
            HideOnFullscreen = _mainWindow.HideOnFullscreen,
            AlbumColouring = _mainWindow.AlbumColouring,
            RunOnStartup = RunOnStartupCheckBox.IsChecked == true,
            Scale = ScaleSlider.Value / 100
        });
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        ScaleTextBox.Text = ScaleSlider.Value.ToString("0");
        SettingChanged(sender, e);
    }

    private void CommitScale()
    {
        if (double.TryParse(ScaleTextBox.Text, out double value) && double.IsFinite(value))
            ScaleSlider.Value = Math.Clamp(Math.Round(value), 10, 300);
        ScaleTextBox.Text = ScaleSlider.Value.ToString("0");
    }

    private void ScaleTextBox_LostFocus(object sender, RoutedEventArgs e) => CommitScale();

    private void ScaleTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { CommitScale(); e.Handled = true; }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => _mainWindow.RefreshMedia();

    private void RestoreDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings();
        LoadControls(defaults);
        _mainWindow.SwitchLayout(defaults.Layout);
        _mainWindow.ApplyTheme(defaults.ThemeIndex);
        SettingChanged(sender, e);
    }

    private void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this,
            "Clear all saved preferences and startup registration, then exit? App files will remain on your device.",
            "Clear saved data", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
        {
            SettingsManager.WipeSettingsAndUninstall();
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();

    private void GitHubBtn_Click(object sender, RoutedEventArgs e) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/Inexplicable05") { UseShellExecute = true });
}
