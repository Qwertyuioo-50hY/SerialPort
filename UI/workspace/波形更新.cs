namespace Tomography.UI.workspace;

public sealed partial class ImagingWorkspaceView
{
    public void ShowChannels(uint[] channels) => ShowMeasurements(channels.Select(value => (double)value).ToArray());
    public void ShowMeasurements(double[] channels) { ChannelWaveform.ShowMeasurements(channels); ChannelTable.SetMeasurements(channels); }
    public void ShowReference(double[] channels) { ChannelWaveform.ShowReference(channels); ChannelTable.SetReference(channels); }
}
