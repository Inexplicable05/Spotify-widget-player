using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Dsp;

namespace SpotiFloat;

public sealed class AudioProcessor : IDisposable
{
    private static readonly Lazy<AudioProcessor> _instance = new(() => new AudioProcessor());
    public static AudioProcessor Instance => _instance.Value;
    public const int BandCount = 28;
    public const int WaveformCount = 64;
    private const int FftSize = 1024;
    public float[] FrequencyBands { get; } = new float[BandCount];
    public float[] WaveformPoints { get; } = new float[WaveformCount];
    public bool IsCapturingReal { get; private set; }
    private readonly object _lock = new();
    private readonly Complex[] _fftBuffer = new Complex[FftSize];
    private readonly float[] _samples = new float[FftSize];
    private int _sampleIndex;
    private int _processId;
    private CancellationTokenSource? _cancellation;
    private Task? _captureTask;
    private bool _disposed;

    private AudioProcessor() { }

    public void SetSource(string? appId, bool isPlaying)
    {
        int processId = isPlaying ? FindAudioProcess(appId) : 0;
        lock (_lock)
        {
            if (_disposed || (processId == _processId && _captureTask is { IsCompleted: false })) return;
            _cancellation?.Cancel();
            _cancellation = null;
            _processId = processId;
            IsCapturingReal = false;
            Clear();
            if (processId == 0) return;
            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            _captureTask = Task.Run(() => CaptureAsync(processId, cancellation));
        }
    }

    private async Task CaptureAsync(int processId, CancellationTokenSource cancellation)
    {
        try
        {
            await ProcessAudioCapture.RunAsync(processId, samples =>
            {
                lock (_lock)
                    if (_cancellation == cancellation && !cancellation.IsCancellationRequested)
                        ProcessSamples(samples);
            }, () =>
            {
                lock (_lock)
                    if (_cancellation == cancellation && !cancellation.IsCancellationRequested)
                        IsCapturingReal = true;
            }, cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex) { Debug.WriteLine($"Application audio capture failed for PID {processId}: {ex}"); }
        finally
        {
            lock (_lock)
            {
                if (_cancellation == cancellation)
                {
                    _cancellation = null;
                    IsCapturingReal = false;
                    Clear();
                }
            }
            cancellation.Dispose();
        }
    }

    internal static int FindAudioProcess(string? appId)
    {
        // shortcut: matches audio owners by executable name; add package matching if a player uses a differently named audio host.
        var names = SupportedMusicSources.GetProcessNames(appId);
        if (names.Length == 0 || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348)) return 0;
        try
        {
            using var devices = new MMDeviceEnumerator();
            foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            using (device)
            {
                var sessions = device.AudioSessionManager.Sessions;
                for (int i = 0; i < sessions.Count; i++)
                using (var session = sessions[i])
                {
                    if (session.State != AudioSessionState.AudioSessionStateActive || session.GetProcessID == 0) continue;
                    try
                    {
                        using var process = Process.GetProcessById((int)session.GetProcessID);
                        if (names.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase)) return process.Id;
                    }
                    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
                    {
                        Debug.WriteLine($"Audio process is unavailable: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Music audio process lookup failed: {ex}"); }
        return 0;
    }

    private void ProcessSamples(float[] samples)
    {
        for (int offset = 0; offset + ProcessAudioCapture.Channels <= samples.Length; offset += ProcessAudioCapture.Channels)
        {
            float sample = (samples[offset] + samples[offset + 1]) / 2;
            _samples[_sampleIndex++] = float.IsFinite(sample) ? sample : 0;
            if (_sampleIndex != FftSize) continue;
            _sampleIndex = 0;
            ProcessFft();
            int step = FftSize / WaveformCount;
            for (int i = 0; i < WaveformCount; i++)
            {
                float sum = 0;
                for (int j = 0; j < step; j++) sum += _samples[i * step + j];
                WaveformPoints[i] = Math.Clamp(sum / step, -1f, 1f);
            }
        }
    }

    private void ProcessFft()
    {
        for (int i = 0; i < FftSize; i++)
        {
            double window = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (FftSize - 1));
            _fftBuffer[i].X = (float)(_samples[i] * window);
            _fftBuffer[i].Y = 0;
        }
        FastFourierTransform.FFT(true, (int)Math.Log2(FftSize), _fftBuffer);
        int halfSize = FftSize / 2;
        for (int b = 0; b < BandCount; b++)
        {
            double low = 40 * Math.Pow(16000.0 / 40, (double)b / BandCount);
            double high = 40 * Math.Pow(16000.0 / 40, (double)(b + 1) / BandCount);
            int binLow = Math.Clamp((int)(low * FftSize / ProcessAudioCapture.SampleRate), 1, halfSize - 1);
            int binHigh = Math.Clamp((int)(high * FftSize / ProcessAudioCapture.SampleRate), binLow + 1, halfSize);
            float sum = 0;
            for (int k = binLow; k < binHigh; k++)
                sum += MathF.Sqrt(_fftBuffer[k].X * _fftBuffer[k].X + _fftBuffer[k].Y * _fftBuffer[k].Y);
            FrequencyBands[b] = Math.Clamp(sum / (binHigh - binLow) * 8f, 0f, 1f);
        }
    }

    private void Clear()
    {
        Array.Clear(FrequencyBands);
        Array.Clear(WaveformPoints);
        Array.Clear(_samples);
        _sampleIndex = 0;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _cancellation?.Cancel();
            _cancellation = null;
            IsCapturingReal = false;
            Clear();
        }
    }
}
