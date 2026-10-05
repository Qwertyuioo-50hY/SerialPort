using System;
using Tomography.Business.Models;
using Tomography.Business.Configuration;

namespace Tomography.Business.Interfaces
{
    /// <summary>
    /// 配置服务接口，负责配置的读取、保存与默认初始化。
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 保存应用配置。
        /// </summary>
        /// <param name="settings">待保存配置。</param>
        /// <param name="filePath">目标文件路径，空则使用默认路径。</param>
        /// <returns>操作结果。</returns>
        OperationResult Save(AppSettings settings, string? filePath = null);

        /// <summary>
        /// 读取应用配置。
        /// </summary>
        /// <param name="filePath">配置文件路径，空则使用默认路径。</param>
        /// <returns>操作结果，成功时 <c>Data</c> 为 <see cref="AppSettings"/>。</returns>
        OperationResult Load(string? filePath = null);

        /// <summary>
        /// 读取配置，若不存在或无效则创建默认配置并返回。
        /// </summary>
        /// <param name="filePath">配置文件路径，空则使用默认路径。</param>
        /// <returns>操作结果，成功时 <c>Data</c> 为 <see cref="AppSettings"/>。</returns>
        OperationResult LoadOrCreateDefault(string? filePath = null);
    }
}
