using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SpotiFloat;
using SpotiFloat.Controls;
using MediaState = SpotiFloat.MediaState;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.InitializeComponent();
        var main = new MainWindow();
        try
        {
            foreach (byte red in new byte[] { 0, 51, 102, 153, 204, 255 })
            foreach (byte green in new byte[] { 0, 51, 102, 153, 204, 255 })
            foreach (byte blue in new byte[] { 0, 51, 102, 153, 204, 255 })
            {
                var background = Color.FromRgb(red, green, blue);
                Check(TextContrast.Ratio(TextContrast.ReadableOn(background), background) >= 4.5, "Text contrast below 4.5:1");
                foreach (var surface in new[] { Color.FromRgb(24, 24, 24), Color.FromRgb(242, 242, 242) })
                {
                    var adjusted = TextContrast.AccentOn(background, surface);
                    Check(TextContrast.Ratio(adjusted, surface) >= 4.5, "Adjusted album colour is unreadable");
                    if (TextContrast.Ratio(background, surface) >= 4.5)
                        Check(adjusted == background, "Already-readable album colour should not change");
                }
            }
            Check(Math.Abs(TextContrast.Ratio(Colors.Black, Colors.White) - 21) < 0.001, "Incorrect contrast formula");
            main.SwitchLayout(PlayerLayout.Bar);
            var host = (ContentControl)main.FindName("PlayerHost");
            Check(host.Focusable, "Player host must support keyboard context-menu access");
            var bar = (BarPlayerControl)host.Content;
            var title = (TextBlock)bar.FindName("TitleText");
            var lastState = typeof(MainWindow).GetField("_lastState", BindingFlags.Instance | BindingFlags.NonPublic)!;
            byte[] redPixel = [0, 0, 180, 255];
            var redArtwork = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, redPixel, 4);
            lastState.SetValue(main, new MediaState { Art = redArtwork, Title = "Album-coloured title" });
            main.EnableDynamicTitleColour = true;
            foreach (int theme in new[] { 0, 1, 2 })
            {
                main.ApplyTheme(theme);
                Check(!DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression,
                    "Static theme did not apply album colour");
                var accent = ((SolidColorBrush)title.Foreground).Color;
                Check(accent.R > accent.G && accent.G == accent.B, "Red artwork lost its hue");
                Check((title.Effect != null) == (theme == 2), "Glass halo leaked into another theme");
            }
            main.EnableDynamicTitleColour = false;
            main.RefreshAppearance();
            Check(title.Effect == null, "Toggle-off left the Glass halo behind");
            lastState.SetValue(main, new MediaState());
            main.ApplyTheme(1);
            main.EnableDynamicTitleColour = true;
            title.Foreground = Brushes.Red;
            main.ApplyTheme(3);
            Check(DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression,
                "Dynamic theme did not restore the title resource reference");
            foreach (var colour in new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Gray, Colors.White, Colors.Black })
            {
                byte[] pixels = [colour.B, colour.G, colour.R, 255];
                var art = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 4);
                lastState.SetValue(main, new MediaState { Art = art, Title = "Contrast test" });
                main.RefreshAppearance();
                var surface = ((SolidColorBrush)app.Resources["AppBackground"]).Color;
                Check(TextContrast.Ratio(((SolidColorBrush)title.Foreground).Color, surface) >= 4.5, "Dynamic title is unreadable");
                var foreground = ((SolidColorBrush)app.Resources["PrimaryTextForeground"]).Color;
                foreach (var key in new[] { "ButtonBackground", "TooltipBackground", "SettingsPopupBackground" })
                    Check(TextContrast.Ratio(foreground, ((SolidColorBrush)app.Resources[key]).Color) >= 4.5, $"Unreadable dynamic surface: {key}");
            }
            main.ApplyTheme(1);
            main.EnableDynamicTitleColour = false;
            title.Foreground = Brushes.Red;
            main.RefreshAppearance();
            Check(DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression, "Toggle-off left stale title colour");
            lastState.SetValue(main, new MediaState());
            main.ApplyTheme(3);
            Check(DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression, "Missing art left stale title colour");
            foreach (var layout in new[] { PlayerLayout.Square, PlayerLayout.Disk, PlayerLayout.Bar })
            {
                main.SwitchLayout(layout);
                Check(host.ContextMenu.Items.OfType<MenuItem>().Select(x => x.Header.ToString()).SequenceEqual(new[] { "_Settings", "E_xit" }),
                    "A player layout lost the shared menu");
            }
            Console.WriteLine("PASS: 216 contrast samples, 432 adjusted accents, album colour in all static themes, halo cleanup, theme transitions, and shared menu.");
            VerifySharedThemes(main, lastState, redArtwork);
            main.SwitchLayout(PlayerLayout.Bar);

            var settings = new SettingsWindow(main) { ShowActivated = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000 };
            try
            {
                // Prevent tests from writing preferences or startup registration.
                typeof(SettingsWindow).GetField("_isInitializing", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, true);
                var slider = (Slider)settings.FindName("ScaleSlider");
                var input = (TextBox)settings.FindName("ScaleTextBox");
                foreach (var value in new[] { "NaN", "Infinity", "bad", "", "9", "301", "125" })
                {
                    input.Text = value;
                    input.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
                    Check(double.IsFinite(slider.Value) && slider.Value >= 10 && slider.Value <= 300, "Invalid scale accepted");
                }
                Check(slider.Value == 125, "Editable percentage did not update size");
                Check(((CheckBox)settings.FindName("LockCheckBox")).Content.ToString() == "Lock position", "Position label is reversed");
                settings.Show();
                string previews = Path.Combine(AppContext.BaseDirectory, "previews");
                Directory.CreateDirectory(previews);
                foreach (var (theme, name) in new[] { (0, "dark"), (1, "light"), (2, "glass"), (3, "dynamic") })
                {
                    if (theme == 3)
                    {
                        var art = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 120, 170, 230, 255 }, 4);
                        lastState.SetValue(main, new MediaState { Art = art });
                    }
                    main.ApplyTheme(theme);
                    ((RadioButton)settings.FindName(new[] { "ThemeDarkBtn", "ThemeLightBtn", "ThemeGlassBtn", "ThemeDynamicBtn" }[theme])).IsChecked = true;
                    typeof(SettingsWindow).GetMethod("UpdateDependentControls", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(settings, null);
                    Check(((CheckBox)settings.FindName("DynamicTitleColourCheckBox")).IsEnabled == (theme != 3),
                        "Album-colour option must be available in Light, Dark, and Glass for Bar");
                    foreach (var layoutName in new[] { "LayoutSquareBtn", "LayoutDiskBtn", "LayoutBarBtn" })
                    {
                        ((RadioButton)settings.FindName(layoutName)).IsChecked = true;
                        typeof(SettingsWindow).GetMethod("UpdateDependentControls", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(settings, null);
                        Check(((CheckBox)settings.FindName("DynamicTitleColourCheckBox")).IsEnabled == (theme != 3), "Title option incorrectly restricted by layout");
                    }
                    settings.UpdateLayout();
                    SavePreview(settings, Path.Combine(previews, name + ".png"));
                }
                ((Expander)settings.FindName("AdvancedExpander")).IsExpanded = true;
                settings.UpdateLayout();
                SavePreview(settings, Path.Combine(previews, "advanced.png"));
                Check(settings.ActualHeight <= settings.MaxHeight, "Settings exceeds available height");
                settings.MaxHeight = 420;
                settings.UpdateLayout();
                var scroll = (ScrollViewer)settings.FindName("SettingsScroller");
                Check(scroll.ScrollableHeight > 0, "Small displays must allow scrolling to all settings");
                scroll.ScrollToEnd();
                settings.UpdateLayout();
                SavePreview(settings, Path.Combine(previews, "small-screen.png"));
                Console.WriteLine($"PASS: percent input validation, lock label, and settings rendered in four themes. Previews: {previews}");
            }
            finally { settings.Close(); }

            main.SwitchLayout(PlayerLayout.Bar);
            bar = (BarPlayerControl)host.Content;
            var previewState = new MediaState { Art = redArtwork, Title = "Album-coloured title", Artist = "Glass contrast preview" };
            lastState.SetValue(main, previewState);
            bar.UpdateMediaState(previewState);
            main.EnableDynamicTitleColour = true;
            main.ApplyTheme(2);
            bar.AppScaleTransform!.ScaleX = bar.AppScaleTransform.ScaleY = 1;
            host.Content = null;
            var backdrop = new Border { Width = 700, Height = 160, Child = bar };
            bar.HorizontalAlignment = HorizontalAlignment.Center;
            bar.VerticalAlignment = VerticalAlignment.Center;
            foreach (var (background, name) in new[] { (Brushes.White, "glass-title-light-backdrop"), (Brushes.Black, "glass-title-dark-backdrop") })
            {
                backdrop.Background = background;
                backdrop.Measure(new Size(700, 160));
                backdrop.Arrange(new Rect(0, 0, 700, 160));
                backdrop.UpdateLayout();
                SavePreview(backdrop, Path.Combine(AppContext.BaseDirectory, "previews", name + ".png"));
            }
        }
        finally
        {
            ((IDisposable)main.FindName("TrayIcon")).Dispose();
            main.Close();
            app.Shutdown();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void VerifySharedThemes(MainWindow main, FieldInfo lastState, BitmapSource artwork)
    {
        var host = (ContentControl)main.FindName("PlayerHost");
        var state = new MediaState { Art = artwork, Title = "Album-coloured title", Artist = "Shared theme check", Position = 45, Duration = 180 };
        string previews = Path.Combine(AppContext.BaseDirectory, "previews");
        Directory.CreateDirectory(previews);
        foreach (var layout in new[] { PlayerLayout.Square, PlayerLayout.Disk })
        {
            main.SwitchLayout(layout);
            var player = (PlayerControlBase)host.Content!;
            host.Content = null;
            player.AppScaleTransform!.ScaleX = player.AppScaleTransform.ScaleY = 1;
            var window = new Window { Content = player, SizeToContent = SizeToContent.WidthAndHeight,
                WindowStyle = WindowStyle.None, ShowActivated = false,
                Left = -10000, Top = -10000, Background = Brushes.DimGray };
            try
            {
                window.Show();
                lastState.SetValue(main, state);
                player.UpdateMediaState(state);
                var image = (Image)player.FindName("AlbumArtImage");
                image.BeginAnimation(UIElement.OpacityProperty, null);
                image.Opacity = 1;
                var title = (TextBlock)player.FindName("TitleText");
                var icon = (System.Windows.Shapes.Path)player.FindName("PlayPauseIcon");
                var hover = (UIElement)player.FindName("HoverGroup");
                player.GetType().GetMethod("ShowHover", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(player, null);
                hover.BeginAnimation(UIElement.OpacityProperty, null);
                hover.Opacity = 1;
                if (player.FindName("HoverOverlay") is UIElement overlay)
                {
                    overlay.BeginAnimation(UIElement.OpacityProperty, null);
                    overlay.Opacity = 1;
                }
                foreach (var (theme, name) in new[] { (0, "dark"), (1, "light"), (2, "glass"), (3, "dynamic") })
                {
                    main.EnableDynamicTitleColour = true;
                    main.ApplyTheme(theme);
                    window.UpdateLayout();
                    Check(((SolidColorBrush)icon.Fill).Color == ((SolidColorBrush)Application.Current.Resources["ButtonIconForeground"]).Color,
                        $"{layout} icon did not follow {name} theme");
                    Brush progress = layout == PlayerLayout.Square
                        ? ((Border)player.FindName("HoverProgressFill")).Background
                        : ((System.Windows.Shapes.Path)((Canvas)player.FindName("ArcCanvas")).Children[1]).Stroke;
                    Check(((SolidColorBrush)progress).Color == ((SolidColorBrush)Application.Current.Resources["SliderThumb"]).Color,
                        $"{layout} progress did not follow {name} theme");
                    var foreground = ((SolidColorBrush)Application.Current.Resources["PrimaryTextForeground"]).Color;
                    if (layout == PlayerLayout.Square)
                    {
                        foreach (System.Windows.Shapes.Rectangle rectangle in ((Canvas)player.FindName("VisualizerCanvas")).Children)
                            Check(((SolidColorBrush)rectangle.Fill).Color == foreground, "Square visualizer ignored theme");
                    }
                    else
                    {
                        var wave = (System.Windows.Shapes.Polyline)((Canvas)player.FindName("WaveformCanvas")).Children[0];
                        Check(((SolidColorBrush)wave.Stroke).Color == foreground, "Disk waveform ignored theme");
                    }
                    Check(DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression == (theme == 3),
                        $"{layout} album title did not follow {name} theme");
                    if (theme != 3)
                    {
                        var accent = ((SolidColorBrush)title.Foreground).Color;
                        Check(accent.R > accent.G && accent.G == accent.B, $"{layout} lost the album title hue");
                    }
                    SavePreview(window, Path.Combine(previews, $"{layout.ToString().ToLowerInvariant()}-{name}.png"));
                    main.EnableDynamicTitleColour = false;
                    main.RefreshAppearance();
                    Check(DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression && title.Effect == null,
                        $"{layout} title toggle did not reset appearance");
                }
                if (layout == PlayerLayout.Square)
                {
                    main.ApplyTheme(0);
                    VerifySquareProgress((SquarePlayerControl)player, window, previews);
                }
                lastState.SetValue(main, new MediaState());
                main.EnableDynamicTitleColour = true;
                main.RefreshAppearance();
                Check(DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression, $"{layout} missing artwork left an accent");
            }
            finally { window.Close(); }
        }
        Console.WriteLine("PASS: live theme changes on existing Square/Disk icons, progress and titles; toggle and missing-art reset. Eight player previews rendered.");
    }

    private static void VerifySquareProgress(SquarePlayerControl player, Window window, string previews)
    {
        var hitArea = (Grid)player.FindName("HoverProgressContainer");
        var rail = (Grid)player.FindName("HoverProgressRail");
        var idle = (Grid)player.FindName("IdleProgressContainer");
        var visualizer = (Canvas)player.FindName("VisualizerCanvas");
        var knob = (FrameworkElement)player.FindName("ProgressKnob");
        var badge = (Border)player.FindName("TimestampBadge");
        var card = (FrameworkElement)player.FindName("CardGrid");
        var artist = (FrameworkElement)player.FindName("ArtistText");
        var seek = typeof(SquarePlayerControl).GetMethod("SeekToMouseX", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var timestamp = typeof(SquarePlayerControl).GetMethod("UpdateTimestamp", BindingFlags.Instance | BindingFlags.NonPublic)!;
        double requested = -1;
        player.SeekRequested += seconds => requested = seconds;
        Check(idle.Height == 3 && rail.Height == 4 && knob.Width == 8 && knob.Height == rail.Height && hitArea.Height >= 24, "Unexpected progress dimensions");
        foreach (double scale in new[] { 1.0, 3.0 })
        {
            player.AppScaleTransform!.ScaleX = player.AppScaleTransform.ScaleY = scale;
            window.UpdateLayout();
            Rect visualizerBounds = visualizer.TransformToAncestor(card).TransformBounds(new Rect(visualizer.RenderSize));
            Rect idleTrackBounds = idle.TransformToAncestor(card).TransformBounds(new Rect(idle.RenderSize));
            Check(Math.Abs(visualizerBounds.Bottom - idleTrackBounds.Top) < 0.01,
                "Square visualizer baseline must touch the idle progress rail");
            foreach (System.Windows.Shapes.Rectangle bar in visualizer.Children)
            {
                Rect barBounds = bar.TransformToAncestor(card).TransformBounds(new Rect(bar.RenderSize));
                Check(Math.Abs(barBounds.Bottom - idleTrackBounds.Top) < 0.01,
                    "Rendered visualizer bar must touch the rail without a gap or overlap");
            }
            foreach (double fraction in new[] { 0.0, 0.05, 0.5, 0.95, 1.0 })
            {
                double x = fraction * hitArea.ActualWidth;
                seek.Invoke(player, new object[] { x });
                timestamp.Invoke(player, new object[] { x });
                window.UpdateLayout();
                Check(Math.Abs(requested - fraction * 180) < 0.01, "Base rail changed the seek position");
                Rect knobBounds = knob.TransformToAncestor(card).TransformBounds(new Rect(knob.RenderSize));
                Rect badgeBounds = badge.TransformToAncestor(card).TransformBounds(new Rect(badge.RenderSize));
                Rect artistBounds = artist.TransformToAncestor(card).TransformBounds(new Rect(artist.RenderSize));
                Rect railBounds = rail.TransformToAncestor(card).TransformBounds(new Rect(rail.RenderSize));
                Rect idleBounds = idle.TransformToAncestor(card).TransformBounds(new Rect(idle.RenderSize));
                Check(Math.Abs(knobBounds.Top - railBounds.Top) < 0.01 && Math.Abs(knobBounds.Bottom - railBounds.Bottom) < 0.01,
                    "Progress marker is not vertically centred on the rail");
                Check(Math.Abs(knobBounds.Left + knobBounds.Width / 2 - (railBounds.Left + railBounds.Width * fraction)) < 0.01,
                    "Progress marker is detached from the fill endpoint");
                Check(card.Clip is RectangleGeometry clip && clip.Rect == new Rect(0, 0, 240, 240) && clip.RadiusX == 14 && clip.RadiusY == 14,
                    "Marker and rail must share the rounded card clip");
                Check(Math.Abs(railBounds.Bottom - card.ActualHeight) < 0.01 && Math.Abs(idleBounds.Bottom - card.ActualHeight) < 0.01,
                    "Progress must touch the very base in idle and hover states");
                Check(Math.Abs(railBounds.Width - card.ActualWidth) < 0.01, "Base rail must span the card width");
                Check(badgeBounds.Left >= 7.9 && badgeBounds.Right <= card.ActualWidth - 7.9, "Timestamp crosses card edges");
                Check(badgeBounds.Top >= artistBounds.Bottom && badgeBounds.Bottom < railBounds.Top, "Timestamp overlaps artist or rail");
                Check(Math.Abs(((Border)player.FindName("HoverProgressFill")).Width - rail.ActualWidth * fraction) < 0.01, "Progress fill misaligned");
                SavePreview(window, Path.Combine(previews, $"square-progress-{scale:0}x-{fraction * 100:0}.png"));
            }
            seek.Invoke(player, new object[] { -100.0 });
            Check(requested == 0, "Seek before rail must clamp to start");
            seek.Invoke(player, new object[] { hitArea.ActualWidth + 100 });
            Check(requested == 180, "Seek after rail must clamp to end");
        }
        player.AppScaleTransform!.ScaleX = player.AppScaleTransform.ScaleY = 1;
        typeof(SquarePlayerControl).GetMethod("ProgressBar_MouseLeave", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(player, new object?[] { hitArea, null });
        Check(badge.Visibility == Visibility.Collapsed, "Timestamp should hide when leaving the seek area");
        Console.WriteLine("PASS: Square marker/rail centre alignment, fill endpoint alignment and seeking at 0/5/50/95/100%, including 300% scale and drag beyond endpoints.");
    }

    private static void SavePreview(FrameworkElement window, string path)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(path);
        encoder.Save(output);
    }
}
