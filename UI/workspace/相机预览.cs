using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace Tomography.UI.workspace;

public sealed class CameraPreviewView : UserControl
{
    private readonly Image _image = new() { Height = 260, Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform };
    private readonly TextBlock _status = new() { Text = "未连接相机", TextWrapping = TextWrapping.Wrap };
    private readonly Button _button = new() { Content = "开启相机" };
    private MediaCapture? _capture;
    private MediaFrameReader? _reader;
    private bool _busy;
    private int _pendingFrame;

    public CameraPreviewView()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(_image); panel.Children.Add(_status); panel.Children.Add(_button);
        Content = panel;
        _button.Click += async (_, _) => { if (_reader is null) await StartAsync(); else await StopAsync(); };
        Unloaded += async (_, _) => await StopAsync();
    }

    private async Task StartAsync()
    {
        if (_busy) return;
        _busy = true; _button.IsEnabled = false;
        try
        {
            _capture = new MediaCapture();
            await _capture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            });
            var source = _capture.FrameSources.Values.FirstOrDefault(item => item.Info.SourceKind == MediaFrameSourceKind.Color)
                ?? throw new InvalidOperationException("未找到可用相机。");
            _reader = await _capture.CreateFrameReaderAsync(source, MediaEncodingSubtypes.Bgra8);
            _reader.FrameArrived += FrameArrived;
            if (await _reader.StartAsync() != MediaFrameReaderStartStatus.Success) throw new InvalidOperationException("相机启动失败。");
            _button.Content = "关闭相机"; _status.Text = "相机已连接";
        }
        catch (Exception ex) { await StopAsync(); _status.Text = ex.Message; }
        finally { _busy = false; _button.IsEnabled = true; }
    }

    private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (Interlocked.Exchange(ref _pendingFrame, 1) != 0) return;
        using var frame = sender.TryAcquireLatestFrame();
        var bitmap = frame?.VideoMediaFrame?.SoftwareBitmap;
        if (bitmap is null) { Interlocked.Exchange(ref _pendingFrame, 0); return; }
        using var converted = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var width = converted.PixelWidth; var height = converted.PixelHeight;
        var pixels = new byte[width * height * 4]; converted.CopyToBuffer(pixels.AsBuffer());
        if (!DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (!ReferenceEquals(sender, _reader)) return;
                var image = new WriteableBitmap(width, height);
                using var stream = image.PixelBuffer.AsStream(); stream.Write(pixels);
                image.Invalidate(); _image.Source = image;
            }
            finally { Interlocked.Exchange(ref _pendingFrame, 0); }
        })) Interlocked.Exchange(ref _pendingFrame, 0);
    }

    private async Task StopAsync()
    {
        var reader = _reader; _reader = null;
        if (reader is not null) { reader.FrameArrived -= FrameArrived; await reader.StopAsync(); reader.Dispose(); }
        _capture?.Dispose(); _capture = null;
        _image.Source = null; _button.Content = "开启相机"; _status.Text = "未连接相机";
    }
}
