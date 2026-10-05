using System.Text.Json;
using Tomography.Business.Models;
using Tomography.Business.Configuration;
using Tomography.Business.Interfaces;
using Tomography.Data;
using Tomography.Data.Interfaces;
using System.Text;

namespace Tomography.Business.Services
{
    /// <summary>
    /// 配置解析、默认配置策略与序列化，数据层仅存取字节。
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly IFileStorage _storage;

        public ConfigService(IFileStorage? storage = null)
        {
            _storage = storage ?? new FileStorage();
        }

        private const string DefaultConfigFileName = "appsettings.json";
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// 保存配置到 JSON 文件。
        /// </summary>
        public OperationResult Save(AppSettings settings, string? filePath = null)
        {
            if (settings is null)
            {
                return Fail("配置对象不能为空。");
            }

            try
            {
                var targetPath = ResolveFilePath(filePath);
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                _storage.Write(targetPath, Encoding.UTF8.GetBytes(json));

                return new OperationResult
                {
                    IsSuccess = true,
                    Message = "配置保存成功。",
                    Data = targetPath
                };
            }
            catch (Exception ex)
            {
                return Fail("保存配置失败。", ex);
            }
        }

        /// <summary>
        /// 从 JSON 文件读取配置。
        /// </summary>
        public OperationResult Load(string? filePath = null)
        {
            try
            {
                var targetPath = ResolveFilePath(filePath);
                if (!_storage.Exists(targetPath))
                {
                    return Fail("配置文件不存在。", data: targetPath);
                }

                var json = Encoding.UTF8.GetString(_storage.Read(targetPath)).TrimStart('\uFEFF');
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings is null || settings.DefaultSerialPortConfig is null)
                {
                    return Fail("配置文件内容为空或格式无效。", data: targetPath);
                }

                return new OperationResult
                {
                    IsSuccess = true,
                    Message = "配置读取成功。",
                    Data = settings
                };
            }
            catch (Exception ex)
            {
                return Fail("读取配置失败。", ex);
            }
        }

        /// <summary>
        /// 读取配置；若不存在或读取失败则创建并保存默认配置。
        /// </summary>
        public OperationResult LoadOrCreateDefault(string? filePath = null)
        {
            var loadResult = Load(filePath);
            if (loadResult.IsSuccess)
            {
                return loadResult;
            }

            var settings = AppSettings.CreateDefault();
            var saveResult = Save(settings, filePath);
            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            return new OperationResult
            {
                IsSuccess = true,
                Message = "已生成默认配置。",
                Data = settings
            };
        }

        /// <summary>
        /// 解析配置文件路径。
        /// </summary>
        private static string ResolveFilePath(string? filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                return filePath;
            }

            return Path.Combine(AppContext.BaseDirectory, DefaultConfigFileName);
        }

        /// <summary>
        /// 构造失败结果对象。
        /// </summary>
        private static OperationResult Fail(string message, Exception? ex = null, object? data = null)
        {
            return new OperationResult
            {
                IsSuccess = false,
                Message = message,
                Exception = ex,
                Data = data
            };
        }
    }
}
