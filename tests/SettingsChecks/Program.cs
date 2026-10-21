using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SpotiFloat;
using SpotiFloat.Controls;
using MediaState = SpotiFloat.MediaState;

internal static class Program
{
    private static readonly string Previews = Path.Combine(AppContext.BaseDirectory, "previews");
    private static readonly FieldInfo LastState = typeof(MainWindow).GetField("_lastState", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [STAThread]
    private static void Main()
    {
        var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.InitializeComponent();
        var main = new MainWindow();
        Directory.CreateDirectory(Previews);
        try
        {
            VerifyContrast();
            VerifySettingsMigration();
            VerifyExtraction();
            VerifyButtonContrast(main);
            VerifyPlayers(main);
            VerifySettings(main);
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

    private static Color BrushColour(FrameworkElement element, string key) => ((SolidColorBrush)element.FindResource(key)).Color;

    private static BitmapSource Artwork(params Color[] colours)
    {
        var pixels = colours.SelectMany(c => new byte[] { c.B, c.G, c.R, c.A }).ToArray();
        return BitmapSource.Create(colours.Length, 1, 96, 96, PixelFormats.Bgra32, null, pixels, colours.Length * 4);
    }

    private static void VerifyContrast()
    {
        foreach (byte red in new byte[] { 0, 51, 102, 153, 204, 255 })
        foreach (byte green in new byte[] { 0, 51, 102, 153, 204, 255 })
        foreach (byte blue in new byte[] { 0, 51, 102, 153, 204, 255 })
        {
            var background = Color.FromRgb(red, green, blue);
            Check(TextContrast.Ratio(TextContrast.ReadableOn(background), background) >= 4.5, "Neutral contrast below 4.5:1");
            foreach (var surface in new[] { Color.FromRgb(24, 24, 24), Color.FromRgb(242, 242, 242) })
            {
                var adjusted = TextContrast.AccentOn(background, surface);
                Check(TextContrast.Ratio(adjusted, surface) >= 4.5, "Adjusted accent is unreadable");
                if (TextContrast.Ratio(background, surface) >= 4.5)
                    Check(adjusted == background, "Already-readable artwork colour changed");
            }
        }
        Check(Math.Abs(TextContrast.Ratio(Colors.Black, Colors.White) - 21) < 0.001, "Incorrect contrast formula");
        Console.WriteLine("PASS: 216 neutral contrast and 432 accent contrast samples; readable accents stay unchanged.");
    }

    private static void VerifySettingsMigration()
    {
        var deserialize = typeof(SettingsManager).GetMethod("DeserializeSettings", BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var (json, theme, mode) in new[]
        {
            ("{}", 0, AlbumColourMode.Off),
            ("{\"ThemeIndex\":3,\"EnableDynamicTitleColour\":false}", 0, AlbumColourMode.Full),
            ("{\"ThemeIndex\":1,\"EnableDynamicTitleColour\":true}", 1, AlbumColourMode.Title),
            ("{\"ThemeIndex\":2,\"EnableDynamicTitleColour\":false}", 2, AlbumColourMode.Off),
            ("{\"ThemeIndex\":2,\"AlbumColouring\":2,\"EnableDynamicTitleColour\":true}", 2, AlbumColourMode.Accents),
            ("{\"ThemeIndex\":99,\"AlbumColouring\":99}", 2, AlbumColourMode.Off)
        })
        {
            var settings = (AppSettings)deserialize.Invoke(null, new object[] { json })!;
            Check(settings.ThemeIndex == theme && settings.AlbumColouring == mode, "Incorrect settings migration: " + json);
            string saved = JsonSerializer.Serialize(settings);
            Check(!saved.Contains("EnableDynamicTitleColour"), "Legacy title flag is still persisted");
            var reloaded = (AppSettings)deserialize.Invoke(null, new object[] { saved })!;
            Check(reloaded.AlbumColouring == mode && reloaded.ThemeIndex == theme, "Settings round trip changed appearance");
        }
        Check(new AppSettings().AlbumColouring == AlbumColourMode.Off, "New users must default to Off");
        Console.WriteLine("PASS: legacy Dynamic/title settings migration, new defaults, invalid mode and JSON round trips (no file/registry writes).");
    }

    private static void VerifyExtraction()
    {
        foreach (var colour in new[] { Colors.Red, Colors.Blue, Colors.Black, Colors.White, Colors.Gray, Color.FromRgb(244, 84, 65) })
            Check(AlbumPalette.Extract(Artwork(colour)) == colour, "Solid artwork colour was modified");
        var sample = Artwork(Colors.Red, Colors.Red, Colors.Red, Colors.Blue, Colors.White);
        Check(AlbumPalette.Extract(sample) == Colors.Red, "Palette averaged colours instead of selecting the dominant group");
        Check(AlbumPalette.Extract(Artwork(Colors.Transparent, Colors.Blue)) == Colors.Blue, "Transparent pixels contaminated palette");
        var rgb = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Rgb24, null, new byte[] { 210, 30, 60 }, 3);
        Check(AlbumPalette.Extract(rgb) == Color.FromRgb(210, 30, 60), "Non-BGRA image decoded incorrectly");
        Console.WriteLine("PASS: original solid colours, dominant group, transparency and RGB24 palette extraction.");
    }

    private static void VerifyButtonContrast(MainWindow main)
    {
        main.SwitchLayout(PlayerLayout.Bar);
        var host = (ContentControl)main.FindName("PlayerHost");
        var player = (BarPlayerControl)host.Content!;
        host.Content = null;
        player.AppScaleTransform!.ScaleX = player.AppScaleTransform.ScaleY = 1;
        var window = new Window { Content = player, SizeToContent = SizeToContent.WidthAndHeight,
            WindowStyle = WindowStyle.None, ShowActivated = false, Left = -10000, Top = -10000 };
        try
        {
            window.Show();
            main.AlbumColouring = AlbumColourMode.Full;
            foreach (int theme in new[] { 0, 1, 2 })
            {
                main.ApplyTheme(theme);
                foreach (byte red in new byte[] { 0, 51, 102, 153, 204, 255 })
                foreach (byte green in new byte[] { 0, 51, 102, 153, 204, 255 })
                foreach (byte blue in new byte[] { 0, 51, 102, 153, 204, 255 })
                {
                    var seed = Color.FromRgb(red, green, blue);
                    var state = new MediaState { Art = Artwork(seed), Title = "Button contrast", Artist = "Full album colouring" };
                    LastState.SetValue(main, state);
                    player.UpdateMediaState(state);
                    main.RefreshAppearance();
                    var body = BrushColour(player, "AppBackground");
                    var circle = BrushColour(player, "ButtonBackground");
                    var icon = BrushColour(player, "ButtonIconForeground");
                    Check(circle.A == 255, "Full Bar circles must be independent of wallpaper beneath the button");
                    Check(TextContrast.Ratio(icon, circle) >= 4.5, "Icon/circle contrast failed");
                    foreach (var backdrop in new[] { Colors.Black, Colors.White })
                    {
                        byte Composite(byte channel, byte behind) => (byte)Math.Round(channel * body.A / 255.0 + behind * (1 - body.A / 255.0));
                        var visibleBody = Color.FromRgb(Composite(body.R, backdrop.R), Composite(body.G, backdrop.G), Composite(body.B, backdrop.B));
                        Check(TextContrast.Ratio(circle, visibleBody) >= 2,
                            $"Circle/body separation failed: theme {theme}, seed {seed}, backdrop {backdrop}");
                    }
                    foreach (string name in new[] { "BtnPrev", "BtnPlayPause", "BtnNext" })
                    {
                        var button = (Button)player.FindName(name);
                        var glyph = (System.Windows.Shapes.Path)button.Content;
                        Check(((SolidColorBrush)button.Background).Color == circle && ((SolidColorBrush)glyph.Fill).Color == icon,
                            "A media button did not receive the container/icon colour pair");
                        Check(((SolidColorBrush)((Border)VisualTreeHelper.GetChild(button, 0)).Background).Color == circle,
                            "Button template ignored the adaptive container");
                    }
                    if (seed == Colors.Black || seed == Colors.Yellow || seed == Colors.White)
                    {
                        foreach (var (backdrop, label) in new[] { (Brushes.Black, "black"), (Brushes.White, "white") })
                        {
                            window.Background = backdrop;
                            window.UpdateLayout();
                            SavePreview(window, Path.Combine(Previews, $"bar-buttons-{theme}-{red}-{green}-{blue}-{label}.png"));
                        }
                    }
                }
            }
            main.AlbumColouring = AlbumColourMode.Title;
            main.RefreshAppearance();
            Check(!player.Resources.Contains("ButtonBackground") && !player.Resources.Contains("ButtonIconForeground"),
                "Leaving Full left adaptive button colours behind");
        }
        finally { window.Close(); }
        Console.WriteLine("PASS: 648 Full Bar palettes; actual circle/body separation >=2:1 on black/white backdrops, icon/circle contrast >=4.5:1, all three templates and mode reset.");
    }

    private static void VerifyPlayers(MainWindow main)
    {
        var host = (ContentControl)main.FindName("PlayerHost");
        Check(host.Focusable, "Player host lost keyboard context-menu access");
        var red = Artwork(Color.FromRgb(244, 84, 65));
        var blue = Artwork(Color.FromRgb(40, 120, 245));
        foreach (var layout in Enum.GetValues<PlayerLayout>())
        {
            main.SwitchLayout(layout);
            var player = (PlayerControlBase)host.Content!;
            host.Content = null;
            player.AppScaleTransform!.ScaleX = player.AppScaleTransform.ScaleY = 1;
            var window = new Window { Content = player, SizeToContent = SizeToContent.WidthAndHeight,
                WindowStyle = WindowStyle.None, ShowActivated = false, Left = -10000, Top = -10000, Background = Brushes.DimGray };
            try
            {
                window.Show();
                Check(host.ContextMenu.Items.OfType<MenuItem>().Select(x => x.Header.ToString()).SequenceEqual(new[] { "_Settings", "E_xit" }), "Shared menu changed");
                var title = (TextBlock)player.FindName("TitleText");
                var icon = (System.Windows.Shapes.Path)player.FindName("PlayPauseIcon");
                foreach (int theme in new[] { 0, 1, 2 })
                {
                    main.AlbumColouring = AlbumColourMode.Off;
                    main.ApplyTheme(theme);
                    var settingsSurface = (Brush)Application.Current.Resources["SettingsPopupBackground"];
                    var settingsText = (Brush)Application.Current.Resources["PrimaryTextForeground"];
                    foreach (var mode in Enum.GetValues<AlbumColourMode>())
                    {
                        foreach (var artwork in new[] { red, blue })
                        {
                            var state = new MediaState { Art = artwork, Title = "Album colouring", Artist = "Shared palette", Position = 45, Duration = 180 };
                            LastState.SetValue(main, state);
                            player.UpdateMediaState(state);
                            main.AlbumColouring = mode;
                            main.RefreshAppearance();
                            player.GetType().GetMethod("ShowHover", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(player, null);
                            foreach (string name in new[] { "HoverGroup", "HoverOverlay" })
                                if (player.FindName(name) is UIElement overlay) { overlay.BeginAnimation(UIElement.OpacityProperty, null); overlay.Opacity = 1; }
                            window.UpdateLayout();
                            Check(ReferenceEquals(settingsSurface, Application.Current.Resources["SettingsPopupBackground"])
                                && ReferenceEquals(settingsText, Application.Current.Resources["PrimaryTextForeground"]), "Album colouring leaked into Settings");
                            Check(ReferenceEquals(((Image)player.FindName("AlbumArtImage")).Source, artwork), "Album colouring replaced artwork");
                            Check(DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression == (mode == AlbumColourMode.Off), "Title mode not respected");
                            Check((title.Effect != null) == (theme == 2 && mode != AlbumColourMode.Off), "Glass title halo leaked");
                            var expected = mode >= AlbumColourMode.Accents
                                ? TextContrast.AccentOn(AlbumPalette.Extract(artwork), AlbumPalette.BackgroundFor(player, theme))
                                : ((SolidColorBrush)Application.Current.Resources["ButtonIconForeground"]).Color;
                            if (layout == PlayerLayout.Bar && mode == AlbumColourMode.Full)
                                Check(TextContrast.Ratio(((SolidColorBrush)icon.Fill).Color, BrushColour(player, "ButtonBackground")) >= 4.5,
                                    "Bar icon must contrast with its actual circle fill");
                            else Check(((SolidColorBrush)icon.Fill).Color == expected, "Media icons ignored colouring mode");
                            Check(BrushColour(player, "SliderThumb") == expected, "Progress ignored colouring mode");
                            Brush progress = layout == PlayerLayout.Bar ? (Brush)player.FindResource("SliderThumb")
                                : layout == PlayerLayout.Square ? ((Border)player.FindName("HoverProgressFill")).Background
                                : ((System.Windows.Shapes.Path)((Canvas)player.FindName("ArcCanvas")).Children[1]).Stroke;
                            Check(((SolidColorBrush)progress).Color == expected, "Rendered progress did not update");
                            if (mode != AlbumColourMode.Off)
                                Check(TextContrast.Ratio(((SolidColorBrush)title.Foreground).Color, AlbumPalette.BackgroundFor(player, theme)) >= 4.5, "Title below contrast target");
                            if (layout == PlayerLayout.Square)
                                foreach (System.Windows.Shapes.Rectangle rectangle in ((Canvas)player.FindName("VisualizerCanvas")).Children)
                                    Check(((SolidColorBrush)rectangle.Fill).Color == BrushColour(player, "PlayerVisualizerForeground"), "Square visualizer ignored mode");
                            if (layout == PlayerLayout.Disk)
                                Check(((SolidColorBrush)((System.Windows.Shapes.Polyline)((Canvas)player.FindName("WaveformCanvas")).Children[0]).Stroke).Color == BrushColour(player, "PlayerVisualizerForeground"), "Disk waveform ignored mode");
                            Check(player.Resources.Contains("AppBackground") == (mode == AlbumColourMode.Full), "Player surface changed in wrong mode");
                            if (mode == AlbumColourMode.Full && theme == 2)
                                Check(BrushColour(player, "AppBackground").A == 64 && ((Color)player.FindResource("PlayerOverlayColour")).A == 204, "Full made Glass opaque");
                            SavePreview(window, Path.Combine(Previews, $"{layout}-{theme}-{mode}-{(ReferenceEquals(artwork, red) ? "red" : "blue")}.png"));
                        }
                    }
                    LastState.SetValue(main, new MediaState());
                    main.RefreshAppearance();
                    Check(!player.Resources.Contains("AppBackground") && !player.Resources.Contains("SliderThumb") && !player.Resources.Contains("PlayerVisualizerForeground")
                        && title.Effect == null && DependencyPropertyHelper.GetValueSource(title, TextBlock.ForegroundProperty).IsExpression,
                        "Missing artwork left stale colours");
                }
                if (layout == PlayerLayout.Square) VerifySquareProgress((SquarePlayerControl)player, window, Previews);
            }
            finally { window.Close(); }
        }
        // Switch layouts while Full is already active, not just after setting it on a loaded control.
        LastState.SetValue(main, new MediaState { Art = red });
        main.AlbumColouring = AlbumColourMode.Full;
        foreach (var layout in Enum.GetValues<PlayerLayout>())
        {
            main.SwitchLayout(layout);
            Check(((FrameworkElement)host.Content!).Resources.Contains("AppBackground"), "Layout switch lost Full colouring");
        }
        Console.WriteLine("PASS: all 36 layout/theme/mode combinations with two artworks, live updates, reset, shared menu and player-scoped resources. 72 previews rendered.");
    }

    private static void VerifySettings(MainWindow main)
    {
        var settings = new SettingsWindow(main) { ShowActivated = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000 };
        try
        {
            var load = typeof(SettingsWindow).GetMethod("LoadControls", BindingFlags.Instance | BindingFlags.NonPublic)!;
            load.Invoke(settings, new object[] { new AppSettings() });
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
            Check(slider.Value == 125, "Editable size failed");
            Check(settings.FindName("ThemeDynamicBtn") == null && settings.FindName("DynamicTitleColourCheckBox") == null, "Old colour controls remain");
            Check(((CheckBox)settings.FindName("LockCheckBox")).Content.ToString() == "Lock position", "Position label reversed");
            settings.Show();
            foreach (int theme in new[] { 0, 1, 2 })
            foreach (var (mode, name) in new[] { (AlbumColourMode.Off, "ColourOffBtn"), (AlbumColourMode.Title, "ColourTitleBtn"), (AlbumColourMode.Accents, "ColourAccentsBtn"), (AlbumColourMode.Full, "ColourFullBtn") })
            {
                main.ApplyTheme(theme);
                ((RadioButton)settings.FindName(new[] { "ThemeDarkBtn", "ThemeLightBtn", "ThemeGlassBtn" }[theme])).IsChecked = true;
                var choice = (RadioButton)settings.FindName(name);
                choice.IsChecked = true;
                typeof(SettingsWindow).GetMethod("UpdateDependentControls", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(settings, null);
                Check(choice.IsEnabled && choice.GroupName == "AlbumColour", "Mode unavailable");
                settings.UpdateLayout();
                Check(((TextBlock)settings.FindName("AlbumColourHint")).ActualHeight > 0, "Mode description missing");
                SavePreview(settings, Path.Combine(Previews, $"settings-{theme}-{mode}.png"));
            }
            ((Expander)settings.FindName("AdvancedExpander")).IsExpanded = true;
            settings.UpdateLayout();
            Check(settings.ActualHeight <= settings.MaxHeight, "Settings exceeds screen height");
            settings.MaxHeight = 420;
            settings.UpdateLayout();
            var scroll = (ScrollViewer)settings.FindName("SettingsScroller");
            Check(scroll.ScrollableHeight > 0, "Small displays cannot reach all settings");
            scroll.ScrollToEnd();
            settings.UpdateLayout();
            SavePreview(settings, Path.Combine(Previews, "settings-small-screen.png"));
            Console.WriteLine("PASS: four mode choices in three themes, size validation and short-screen scrolling; no preference/registry writes.");
        }
        finally { settings.Close(); }
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
