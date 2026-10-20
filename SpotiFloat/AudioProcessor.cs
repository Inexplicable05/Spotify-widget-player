using System;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.Dsp;

namespace SpotiFloat
{
    /// <summary>
    /// Singleton that captures system audio output via WASAPI loopback and
    /// provides real-time frequency band data (for bar visualizer) and
    /// waveform point data (for disk waveform visualizer).
    /// Silently falls back to simulated data if WASAPI capture is unavailable.
    /// </summary>
    public sealed class AudioProcessor : IDisposable
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        private static readonly Lazy<AudioProcessor> _instance =
            new(() => new AudioProcessor());
        public static AudioProcessor Instance => _instance.Value;

        // ── Public Data (read on UI thread, written on audio thread) ──────────
        public const int BandCount     = 28;
        public const int WaveformCount = 64;

        private readonly float[] _bands         = new float[BandCount];
        private readonly float[] _bandTargets   = new float[BandCount];
        private readonly float[] _waveform       = new float[WaveformCount];
        private readonly float[] _waveformTarget = new float[WaveformCount];
        private readonly object  _lock           = new();

        // Thread-safe snapshots the UI reads each frame
        public float[] FrequencyBands { get; } = new float[BandCount];
        public float[] WaveformPoints { get; } = new float[WaveformCount];
        public bool IsCapturingReal   { get; private set; }
        public bool IsPlaying         { get; set; } // set by MainWindow

        // ── NAudio ────────────────────────────────────────────────────────────
        private WasapiLoopbackCapture? _capture;
        private bool _disposed;

        // FFT
        private const int FftSize     = 1024;
        private readonly Complex[]   _fftBuffer   = new Complex[FftSize];
        private readonly float[]     _sampleAccum = new float[FftSize];
        private int _sampleIndex;

        // ── Simulation (fallback) ─────────────────────────────────────────────
        private readonly Random     _rng        = new();
        private readonly float[]    _simBands   = new float[BandCount];
        private readonly float[]    _simWave    = new float[WaveformCount];
        private double              _simTime;

        private AudioProcessor() { }

        // ─────────────────────────────────────────────────────────────────────
        // Start / Stop
        // ─────────────────────────────────────────────────────────────────────

        public void Start()
        {
            if (_capture != null || _disposed) return;
            try
            {
                _capture                    = new WasapiLoopbackCapture();
                _capture.DataAvailable      += OnDataAvailable;
                _capture.RecordingStopped   += OnRecordingStopped;
                _capture.StartRecording();
                IsCapturingReal = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioProcessor] WASAPI start failed: {ex.Message}. Using simulation.");
                _capture        = null;
                IsCapturingReal = false;
            }
        }

        public void Stop()
        {
            try { _capture?.StopRecording(); } catch { }
        }

        // ─────────────────────────────────────────────────────────────────────
        // WASAPI callback
        // ─────────────────────────────────────────────────────────────────────

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            // e.Buffer contains interleaved float32 samples (2ch typically)
            int bytesPerSample = _capture!.WaveFormat.BitsPerSample / 8;
            int channels       = _capture.WaveFormat.Channels;
            int stride         = bytesPerSample * channels;

            for (int offset = 0; offset + stride <= e.BytesRecorded; offset += stride)
            {
                // Mono-mix: average channels
                float sample = 0;
                for (int ch = 0; ch < channels; ch++)
                    sample += BitConverter.ToSingle(e.Buffer, offset + ch * bytesPerSample);
                sample /= channels;

                _sampleAccum[_sampleIndex] = sample;
                _sampleIndex++;

                if (_sampleIndex >= FftSize)
                {
                    _sampleIndex = 0;
                    ProcessFft();
                    ProcessWaveform(_sampleAccum);
                }
            }
        }

        private void ProcessFft()
        {
            // Fill complex FFT buffer with Hamming window
            for (int i = 0; i < FftSize; i++)
            {
                double window = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (FftSize - 1));
                _fftBuffer[i].X = (float)(_sampleAccum[i] * window);
                _fftBuffer[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(FftSize, 2), _fftBuffer);

            // Map FFT bins → BandCount frequency bands (logarithmic spacing)
            int halfSize = FftSize / 2;
            for (int b = 0; b < BandCount; b++)
            {
                // Log-spaced band edges from ~40Hz to ~16kHz
                double freqLow  = 40  * Math.Pow(16000.0 / 40,  (double)b       / BandCount);
                double freqHigh = 40  * Math.Pow(16000.0 / 40,  (double)(b + 1) / BandCount);
                int    binLow   = (int)(freqLow  / (_capture!.WaveFormat.SampleRate / (double)FftSize));
                int    binHigh  = (int)(freqHigh / (_capture.WaveFormat.SampleRate  / (double)FftSize));
                binLow  = Math.Clamp(binLow,  1, halfSize - 1);
                binHigh = Math.Clamp(binHigh, binLow + 1, halfSize);

                float sum = 0;
                for (int k = binLow; k < binHigh; k++)
                {
                    float mag = (float)Math.Sqrt(_fftBuffer[k].X * _fftBuffer[k].X +
                                                  _fftBuffer[k].Y * _fftBuffer[k].Y);
                    sum += mag;
                }
                float avg = sum / (binHigh - binLow);
                // Scale and clamp to 0–1
                _bandTargets[b] = Math.Clamp(avg * 8f, 0f, 1f);
            }

            lock (_lock)
                Array.Copy(_bandTargets, FrequencyBands, BandCount);
        }

        private void ProcessWaveform(float[] samples)
        {
            // Downsample raw samples to WaveformCount points
            int step = FftSize / WaveformCount;
            for (int i = 0; i < WaveformCount; i++)
            {
                float avg = 0;
                for (int j = 0; j < step; j++)
                    avg += samples[i * step + j];
                _waveformTarget[i] = Math.Clamp(avg / step, -1f, 1f);
            }
            lock (_lock)
                Array.Copy(_waveformTarget, WaveformPoints, WaveformCount);
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                Debug.WriteLine($"[AudioProcessor] Recording stopped with error: {e.Exception.Message}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Simulation fallback — call this from CompositionTarget.Rendering
        // when IsCapturingReal == false
        // ─────────────────────────────────────────────────────────────────────

        public void TickSimulation(double deltaSeconds)
        {
            if (IsCapturingReal) return;

            _simTime += deltaSeconds;

            if (!IsPlaying)
            {
                // Decay toward silence
                for (int i = 0; i < BandCount; i++)
                    FrequencyBands[i] = FrequencyBands[i] * 0.85f;
                for (int i = 0; i < WaveformCount; i++)
                    WaveformPoints[i] = WaveformPoints[i] * 0.9f;
                return;
            }

            // Simulate bands — each band oscillates at its own rate
            for (int i = 0; i < BandCount; i++)
            {
                double t = _simTime * (0.8 + i * 0.12);
                double v = 0.35 + 0.55 * (0.5 + 0.5 * Math.Sin(t * 1.7 + i * 0.4))
                                * (0.5 + 0.5 * Math.Sin(t * 3.1 + i * 0.9));
                _simBands[i] = (float)v;
            }
            // Smooth toward simulated target
            for (int i = 0; i < BandCount; i++)
                FrequencyBands[i] += (_simBands[i] - FrequencyBands[i]) * 0.18f;

            // Simulate waveform — sine wave that varies in amplitude
            double amplitude = 0.4 + 0.5 * (0.5 + 0.5 * Math.Sin(_simTime * 0.7));
            for (int i = 0; i < WaveformCount; i++)
            {
                double x = (double)i / WaveformCount;
                _simWave[i] = (float)(amplitude * Math.Sin(x * Math.PI * 4 + _simTime * 3));
            }
            for (int i = 0; i < WaveformCount; i++)
                WaveformPoints[i] += (_simWave[i] - WaveformPoints[i]) * 0.15f;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Dispose
        // ─────────────────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _capture?.StopRecording(); } catch { }
            _capture?.Dispose();
            _capture = null;
        }
    }
}
