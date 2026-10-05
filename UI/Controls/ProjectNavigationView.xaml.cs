using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;

namespace Tomography.UI.Controls;

public sealed partial class ProjectNavigationView : UserControl
{
    public ProjectNavigationView() => InitializeComponent();
    public ImportedAsset? SelectedAsset => Files.SelectedItem is ListViewItem item ? item.Tag as ImportedAsset : null;
    public void AddAsset(ImportedAsset asset)
    {
        var item = new ListViewItem { Content = asset.Name, Tag = asset };
        Files.Items.Add(item);
        Files.SelectedItem = item;
        EmptyMessage.Visibility = Visibility.Collapsed;
    }
}
