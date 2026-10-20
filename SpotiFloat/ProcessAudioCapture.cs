using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using NAudio.Wave;

namespace SpotiFloat;

// Uses Microsoft's ApplicationLoopback activation contract; format is fixed for the shared analyser.
internal static class ProcessAudioCapture
{
    internal const int SampleRate = 48000;
    internal const int Channels = 2;

    internal static async Task RunAsync(int processId, Action<float[]> onSamples,
        Action onStarted, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
            throw new PlatformNotSupportedException("Application audio capture requires Windows build 20348 or later.");

        var handler = new ActivationHandler();
        IntPtr parameters = Marshal.AllocHGlobal(12);
        IntPtr variant = Marshal.AllocHGlobal(Marshal.SizeOf<ActivationVariant>());
        AudioClient client;
        try
        {
            Marshal.Copy(new[] { 1, processId, 0 }, 0, parameters, 3); // ProcessLoopback, PID, IncludeTargetProcessTree
            Marshal.StructureToPtr(new ActivationVariant
            {
                Type = 65, // VT_BLOB
                Blob = new ActivationBlob { Size = 12, Data = parameters }
            }, variant, false);
            ActivateAudioInterfaceAsync("VAD\\Process_Loopback", typeof(IAudioClient).GUID,
                variant, handler, out var operation);
            client = await handler.Completion.Task.ConfigureAwait(false);
            GC.KeepAlive(operation);
        }
        finally
        {
            Marshal.FreeHGlobal(variant);
            Marshal.FreeHGlobal(parameters);
        }

        using (client)
        {
            cancellationToken.ThrowIfCancellationRequested();
            client.Initialize(AudioClientShareMode.Shared,
                AudioClientStreamFlags.Loopback | AudioClientStreamFlags.EventCallback,
                200000, 0, WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels), Guid.Empty);
            using var available = new EventWaitHandle(false, EventResetMode.AutoReset);
            client.SetEventHandle(available.SafeWaitHandle.DangerousGetHandle());
            var capture = client.AudioCaptureClient;
            client.Start();
            try
            {
                onStarted();
                var handles = new[] { available, cancellationToken.WaitHandle };
                while (WaitHandle.WaitAny(handles, 100) != 1)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    while (capture.GetNextPacketSize() > 0)
                    {
                        var data = capture.GetBuffer(out int frames, out AudioClientBufferFlags flags);
                        try
                        {
                            var samples = new float[frames * Channels];
                            if ((flags & AudioClientBufferFlags.Silent) == 0)
                                Marshal.Copy(data, samples, 0, samples.Length);
                            cancellationToken.ThrowIfCancellationRequested();
                            onSamples(samples);
                        }
                        finally { capture.ReleaseBuffer(frames); }
                    }
                }
            }
            finally { client.Stop(); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ActivationBlob { internal int Size; internal IntPtr Data; }

    [StructLayout(LayoutKind.Explicit)]
    private struct ActivationVariant
    {
        [FieldOffset(0)] internal ushort Type;
        [FieldOffset(8)] internal ActivationBlob Blob;
    }

    [ComImport, Guid("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAgileObject { }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    private sealed class ActivationHandler : IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        internal readonly TaskCompletionSource<AudioClient> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation operation)
        {
            try
            {
                operation.GetActivateResult(out int result, out object activated);
                Marshal.ThrowExceptionForHR(result);
                Completion.TrySetResult(new AudioClient((IAudioClient)activated));
            }
            catch (Exception ex) { Completion.TrySetException(ex); }
        }
    }

    [DllImport("Mmdevapi.dll", ExactSpelling = true, PreserveSig = false)]
    private static extern void ActivateAudioInterfaceAsync(
        [MarshalAs(UnmanagedType.LPWStr)] string path,
        [MarshalAs(UnmanagedType.LPStruct)] Guid iid, IntPtr parameters,
        IActivateAudioInterfaceCompletionHandler handler, out IActivateAudioInterfaceAsyncOperation operation);
}
