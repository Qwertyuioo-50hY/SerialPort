using System.IO;
using Tomography.Business.Models;
using Microsoft.UI.Xaml.Data;

namespace Tomography.UI.Navigation;

public sealed class ProjectNodePresentation : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AiConversation chat) return parameter?.ToString() == "Icon" ? "\uE8F2" : chat.Title;
        var path = value?.ToString() ?? "";
        return parameter?.ToString() == "Icon"
            ? (Directory.Exists(path) || path is "实验输入" or "聊天" ? "\uE8B7" : "\uE8A5")
            : Path.GetFileName(path);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
