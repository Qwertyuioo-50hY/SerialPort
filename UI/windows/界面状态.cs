using System.ComponentModel;

namespace Tomography.UI.ViewModels
{
    /// <summary>界面设计阶段只保存展示状态，不调用设备、文件或算法服务。</summary>
    public sealed class WindowStateViewModel : INotifyPropertyChanged
    {
        private string _section = "接口/协议";
        private bool _parametersVisible = true;
        private string _accountName = "";
        private string? _avatarUri;
        public string AccountName { get => _accountName; set { _accountName = value; Notify(nameof(AccountName)); } }
        public string? AvatarUri { get => _avatarUri; set { _avatarUri = value; Notify(nameof(AvatarUri)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        public string Section { get => _section; set { _section = value; Notify(nameof(Section)); } }
        public bool ParametersVisible { get => _parametersVisible; set { _parametersVisible = value; Notify(nameof(ParametersVisible)); } }

        public void SelectSection(string section)
        {
            if (Section == section)
                ParametersVisible = !ParametersVisible;
            else
            {
                Section = section;
                ParametersVisible = true;
            }
        }

        private void Notify(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
