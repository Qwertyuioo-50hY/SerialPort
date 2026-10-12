using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;

namespace Tomography.UI.Navigation;

public sealed partial class ImportedFilesView : UserControl
{
    public ImportedFilesView() => InitializeComponent();
    public event EventHandler<string>? WindowCommandRequested;
    private void WindowCommandClicked(object sender, RoutedEventArgs e) =>
        WindowCommandRequested?.Invoke(this, (string)((Button)sender).Tag);
    public ImportedAsset? SelectedAsset => Files.SelectedItem is ListViewItem item ? item.Tag as ImportedAsset : null;
    public void AddAsset(ImportedAsset asset)
    {
        if (Files.Items.OfType<ListViewItem>().Any(item => item.Tag is ImportedAsset existing && existing.Path == asset.Path)) return;
        var item = new ListViewItem { Content = asset.Name, Tag = asset };
        Files.Items.Add(item);
        Files.SelectedItem = item;
        EmptyMessage.Visibility = Visibility.Collapsed;
    }
}
