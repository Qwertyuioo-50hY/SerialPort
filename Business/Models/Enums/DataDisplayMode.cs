using System;
namespace Tomography.Business.Models.Enums
{
    /// <summary>
    /// 数据保存模式。
    /// </summary>
    public enum DataDisplayMode
    {
        /// <summary>
        /// 文本模式，数据以文本形式显示。
        /// </summary>
        Text = 0,
        /// <summary>
        /// 十六进制模式，数据以十六进制形式显示，每个字节用两位十六进制数表示，字节之间用空格分隔。
        /// </summary>
        Hex = 1
    }
}
