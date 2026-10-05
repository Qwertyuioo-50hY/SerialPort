using System;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Interfaces
{
    /// <summary>
    /// 校验服务接口，负责计算和验证报文校验值。
    /// </summary>
    public interface IChecksumService
    {
        /// <summary>
        /// 计算指定校验类型对应的校验字节。
        /// </summary>
        /// <param name="data">原始数据，允许为空。</param>
        /// <param name="checksumType">校验类型。</param>
        /// <returns>校验字节数组。</returns>
        byte[] ComputeChecksumBytes(byte[]? data, ChecksumType checksumType);

        /// <summary>
        /// 计算校验值并按十六进制字符串返回。
        /// </summary>
        /// <param name="data">原始数据，允许为空。</param>
        /// <param name="checksumType">校验类型。</param>
        /// <param name="upperCase">是否输出大写十六进制。</param>
        /// <param name="separator">字节分隔符。</param>
        /// <returns>十六进制格式校验值。</returns>
        string ComputeChecksumHex(byte[]? data, ChecksumType checksumType, bool upperCase = true, string separator = " ");

        /// <summary>
        /// 校验数据的实际校验值是否与期望值一致。
        /// </summary>
        /// <param name="data">原始数据，允许为空。</param>
        /// <param name="checksumType">校验类型。</param>
        /// <param name="expectedChecksum">期望校验值。</param>
        /// <returns>一致返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        bool ValidateChecksum(byte[]? data, ChecksumType checksumType, byte[]? expectedChecksum);
    }
}
