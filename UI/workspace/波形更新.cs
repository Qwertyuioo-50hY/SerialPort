namespace Tomography.UI.Controls;

public sealed partial class ImagingWorkspaceView
{
    public void ShowChannels(uint[] channels) => ChannelWaveform.ShowChannels(channels);
    public void ShowMeasurements(double[] channels) => ChannelWaveform.ShowMeasurements(channels);
}
