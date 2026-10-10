using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.windows;

public sealed class ResizeDivider : UserControl
{
    public ResizeDivider() => ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
}
