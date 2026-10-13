using System.IO;
using Microsoft.UI.Xaml.Data;

namespace Tomography.UI.Navigation;

public sealed class ProjectNodePresentation : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var path = value?.ToString() ?? "";
        return parameter?.ToString() == "Icon"
            ? (Directory.Exists(path) || path == "实验输入" ? "\uE8B7" : "\uE8A5")
            : Path.GetFileName(path);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
