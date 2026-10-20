using SpotiFloat;
using Windows.Media.Control;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

if (args.Length == 2 && args[0] == "--tone")
{
    using var output = new WaveOutEvent();
    output.Init(new SignalGenerator(48000, 2) { Frequency = double.Parse(args[1]), Gain = 0.02, Type = SignalGeneratorType.Sin });
    output.Play();
    Console.WriteLine("ready");
    await Task.Delay(5000);
    return;
}

string[] allowed = ["Spotify", "SPOTIFY.EXE", "SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify",
    "AppleMusic.exe", "AppleInc.AppleMusic_test!App", "Amazon Music.exe", "AmazonMusic",
    "AmazonMobileLLC.AmazonMusic_test!App", "AMZNMobileLLC.AmazonMusic_test!App",
    "Deezer", "DEEZER.EXE", "Deezer.62021768415AF_test!App", "TIDAL", "TIDAL.exe", "TIDALMusicAS.TIDAL_test!App"];
string?[] blocked = [null, "", "Chrome", "MSEdge", "firefox.exe", "brave.exe", "opera.exe",
    "vivaldi.exe", "Netflix", "YouTube", "Spotify Web Player", "AppleMusicBrowser",
    "NotSpotify.exe", "AmazonMusicHelper.exe", "SpotifyAB.SpotifyMusicFake_test!App",
    "TIDAL Web Player", "DeezerBrowser", "OperaSoftware.OperaWebBrowser.1772665278"];
foreach (var appId in allowed)
    if (!SupportedMusicSources.IsAllowed(appId)) throw new Exception($"Rejected music app: {appId}");
foreach (var appId in blocked)
    if (SupportedMusicSources.IsAllowed(appId)) throw new Exception($"Allowed unwanted source: {appId}");
Console.WriteLine($"PASS: {allowed.Length} allowed and {blocked.Length} blocked source identities.");
foreach (var appId in allowed)
    if (SupportedMusicSources.GetProcessNames(appId).Length == 0) throw new Exception($"No audio process mapping: {appId}");
foreach (var appId in blocked)
    if (SupportedMusicSources.GetProcessNames(appId).Length != 0 || AudioProcessor.FindAudioProcess(appId) != 0)
        throw new Exception($"Unwanted source has an audio process: {appId}");
Console.WriteLine("PASS: allowed sources have process mappings; blocked sources cannot select audio capture.");
var analyser = AudioProcessor.Instance;
Array.Fill(analyser.FrequencyBands, 1f);
Array.Fill(analyser.WaveformPoints, 1f);
analyser.SetSource("Spotify.exe", false);
if (analyser.IsCapturingReal || analyser.FrequencyBands.Any(x => x != 0) || analyser.WaveformPoints.Any(x => x != 0))
    throw new Exception("Paused playback did not clear visualization.");
Array.Fill(analyser.FrequencyBands, 1f);
analyser.SetSource("OperaSoftware.OperaWebBrowser.1772665278", true);
if (analyser.IsCapturingReal || analyser.FrequencyBands.Any(x => x != 0))
    throw new Exception("Blocked source did not leave visualization quiet.");
Console.WriteLine("PASS: pause and blocked-source transitions clear visualization without simulated fallback.");

if (args.Contains("--live"))
{
    var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
    foreach (var session in manager.GetSessions())
        Console.WriteLine($"{session.SourceAppUserModelId}: {(SupportedMusicSources.IsAllowed(session.SourceAppUserModelId) ? "allowed" : "blocked")}, audio PID {AudioProcessor.FindAudioProcess(session.SourceAppUserModelId)}");
}

if (args.Contains("--capture-check"))
{
    Process StartTone(int frequency) => Process.Start(new ProcessStartInfo(Environment.ProcessPath!)
    {
        ArgumentList = { "--tone", frequency.ToString() },
        UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
    })!;
    using var target = StartTone(440);
    using var other = StartTone(880);
    try
    {
        if (await target.StandardOutput.ReadLineAsync() != "ready" || await other.StandardOutput.ReadLineAsync() != "ready")
            throw new Exception("Test tone failed to start.");
        var captured = new List<float>();
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await Task.Run(() => ProcessAudioCapture.RunAsync(target.Id, packet =>
            {
                for (int i = 0; i < packet.Length; i += 2) captured.Add(packet[i]);
            }, () => Console.WriteLine("Process capture started."), cancel.Token));
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        double Magnitude(int frequency)
        {
            double real = 0, imaginary = 0;
            for (int i = 0; i < captured.Count; i++)
            {
                double angle = 2 * Math.PI * frequency * i / ProcessAudioCapture.SampleRate;
                real += captured[i] * Math.Cos(angle);
                imaginary += captured[i] * Math.Sin(angle);
            }
            return Math.Sqrt(real * real + imaginary * imaginary) / Math.Max(1, captured.Count);
        }
        double wanted = Magnitude(440), unwanted = Magnitude(880);
        Console.WriteLine($"Captured {captured.Count} samples; target magnitude {wanted:F6}; other process {unwanted:F6}.");
        if (wanted < 0.001 || unwanted > wanted * 0.05)
            throw new Exception("Process capture failed to isolate the target tone.");
        Console.WriteLine("PASS: captured target process tone; rejected simultaneous tone from another process.");
    }
    finally
    {
        if (!target.HasExited) target.Kill();
        if (!other.HasExited) other.Kill();
    }
}
